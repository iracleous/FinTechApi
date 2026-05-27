using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FinTechApi.Models;

namespace FinTechApi.Services;

/// <summary>
/// Service that accepts customer orders for trades and exposes order lifecycle operations.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Places a new order on behalf of a customer/account.
    /// Returns the created <see cref="Order"/> (with Id and initial status).
    /// </summary>
    Task<Order> PlaceOrderAsync(
        Guid customerId,
        Guid accountId,
        string instrumentSymbol,
        OrderSide side,
        int quantity,
        decimal? limitPrice = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to cancel an existing order. If cancellation succeeds the order status should be updated.
    /// Returns true if cancellation was applied, false otherwise (already executed or cannot cancel).
    /// </summary>
    Task<bool> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single order by id.
    /// </summary>
    Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists orders optionally filtered by customer or account.
    /// </summary>
    Task<IReadOnlyList<Order>> ListOrdersAsync(
        Guid? customerId = null,
        Guid? accountId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes (or routes for execution) an order and returns the resulting trade(s).
    /// Implementations may return one or more trades for a single order.
    /// </summary>
    Task<IReadOnlyList<Trade>> ExecuteOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}