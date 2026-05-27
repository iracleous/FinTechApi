using FinTechApi.Models;
using FinTechApi.Repositories;

namespace FinTechApi.Services;

/// <summary>
/// Minimal market price provider abstraction used by the service.
/// Implement and register this when executing market orders.
/// </summary>
public interface IMarketPriceProvider
{
    Task<decimal> GetPriceAsync(string instrumentSymbol, CancellationToken cancellationToken = default);
}

public class OrderService : IOrderService
{
    private readonly IAsyncRepository<Order, Guid> _orderRepo;
    private readonly IAsyncRepository<Trade, Guid> _tradeRepo;
    private readonly IAsyncRepository<Account, Guid> _accountRepo;
    private readonly IAsyncRepository<Customer, Guid> _customerRepo;
    private readonly IAsyncRepository<Instrument, string> _instrumentRepo;
    private readonly IMarketPriceProvider? _priceProvider;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IAsyncRepository<Order, Guid> orderRepo,
        IAsyncRepository<Trade, Guid> tradeRepo,
        IAsyncRepository<Account, Guid> accountRepo,
        IAsyncRepository<Customer, Guid> customerRepo,
        IAsyncRepository<Instrument, string> instrumentRepo,
        ILogger<OrderService> logger,
        IMarketPriceProvider? priceProvider = null)
    {
        _orderRepo = orderRepo ?? throw new ArgumentNullException(nameof(orderRepo));
        _tradeRepo = tradeRepo ?? throw new ArgumentNullException(nameof(tradeRepo));
        _accountRepo = accountRepo ?? throw new ArgumentNullException(nameof(accountRepo));
        _customerRepo = customerRepo ?? throw new ArgumentNullException(nameof(customerRepo));
        _instrumentRepo = instrumentRepo ?? throw new ArgumentNullException(nameof(instrumentRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _priceProvider = priceProvider; // optional; required for market orders
    }

    public async Task<Order> PlaceOrderAsync(
        Guid customerId,
        Guid accountId,
        string instrumentSymbol,
        OrderSide side,
        int quantity,
        decimal? limitPrice = null,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (string.IsNullOrWhiteSpace(instrumentSymbol)) throw new ArgumentNullException(nameof(instrumentSymbol));

        var customer = await _customerRepo.GetByIdAsync(customerId, cancellationToken).ConfigureAwait(false)
                     ?? throw new InvalidOperationException("Customer not found.");

        var account = await _accountRepo.GetByIdAsync(accountId, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Account not found.");

        if (account.CustomerId != customer.Id)
            throw new InvalidOperationException("Account does not belong to customer.");

        var instrument = await _instrumentRepo.GetByIdAsync(instrumentSymbol, cancellationToken).ConfigureAwait(false)
                         ?? throw new InvalidOperationException("Instrument not found.");

        var order = new Order
        {
            AccountId = accountId,
            InstrumentSymbol = instrumentSymbol,
            Side = side,
            Quantity = quantity,
            LimitPrice = limitPrice,
            Status = OrderStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        await _orderRepo.AddAsync(order, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Placed order {OrderId} for account {AccountId}", order.Id, accountId);
        return order;
    }

    public async Task<bool> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (order == null) return false;

        if (order.Status == OrderStatus.Filled || order.Status == OrderStatus.Canceled)
        {
            _logger.LogDebug("Order {OrderId} cannot be canceled because it is {Status}", orderId, order.Status);
            return false;
        }

        order.Status = OrderStatus.Canceled;
        await _orderRepo.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Order {OrderId} canceled", orderId);
        return true;
    }

    public async Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _orderRepo.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Order>> ListOrdersAsync(Guid? customerId = null, Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        if (accountId.HasValue)
        {
            return await _orderRepo.FindAsync(o => o.AccountId == accountId.Value, cancellationToken).ConfigureAwait(false);
        }

        if (customerId.HasValue)
        {
            var accounts = await _accountRepo.FindAsync(a => a.CustomerId == customerId.Value, cancellationToken).ConfigureAwait(false);
            var accountIds = accounts.Select(a => a.Id).ToHashSet();

            if (!accountIds.Any()) return Array.Empty<Order>();

            return await _orderRepo.FindAsync(o => accountIds.Contains(o.AccountId), cancellationToken).ConfigureAwait(false);
        }

        return await _orderRepo.ListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Trade>> ExecuteOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdAsync(orderId, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("Order not found.");

        if (order.Status == OrderStatus.Canceled || order.Status == OrderStatus.Filled)
            throw new InvalidOperationException($"Order cannot be executed in status {order.Status}.");

        // Determine execution price
        decimal execPrice;
        if (order.LimitPrice.HasValue)
        {
            execPrice = order.LimitPrice.Value;
        }
        else if (_priceProvider != null)
        {
            execPrice = await _priceProvider.GetPriceAsync(order.InstrumentSymbol, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new InvalidOperationException("Market orders require an IMarketPriceProvider to be registered.");
        }

        // For simplicity: execute entire remaining quantity as a single trade
        var trade = new Trade
        {
            AccountId = order.AccountId,
            InstrumentSymbol = order.InstrumentSymbol,
            Quantity = order.Quantity,
            Price = execPrice,
            Commission = CalculateCommission(order, execPrice),
            Side = order.Side,
            Timestamp = DateTime.UtcNow,
            OrderId = order.Id
        };

        // Persist trade
        await _tradeRepo.AddAsync(trade, cancellationToken).ConfigureAwait(false);

        // Update order
        order.TradeIds.Add(trade.Id);
        order.Status = OrderStatus.Filled;
        order.ExecutedAt = DateTime.UtcNow;
        await _orderRepo.UpdateAsync(order, cancellationToken).ConfigureAwait(false);

        // Update account balance (very simplified bookkeeping)
        var account = await _accountRepo.GetByIdAsync(order.AccountId, cancellationToken).ConfigureAwait(false)
                      ?? throw new InvalidOperationException("Account not found.");

        var gross = trade.Quantity * trade.Price;
        if (order.Side == OrderSide.Buy)
        {
            var debit = gross + trade.Commission;
            if (account.Balance < debit)
                throw new InvalidOperationException("Insufficient funds to execute buy order.");

            account.Balance -= debit;
        }
        else // Sell
        {
            account.Balance += (gross - trade.Commission);
        }

        await _accountRepo.UpdateAsync(account, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Executed order {OrderId} as trade {TradeId} at {Price}", order.Id, trade.Id, trade.Price);

        return new List<Trade> { trade };
    }

    private static decimal CalculateCommission(Order order, decimal executionPrice)
    {
        // Simple flat commission example or percentage as placeholder
        const decimal percent = 0.001m; // 0.1%
        return Math.Round(order.Quantity * executionPrice * percent, 2);
    }
}