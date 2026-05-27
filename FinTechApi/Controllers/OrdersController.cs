 using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FinTechApi.Models;
using FinTechApi.Services;

namespace FinTechApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public class PlaceOrderRequest
        {
            public Guid CustomerId { get; set; }
            public Guid AccountId { get; set; }
            public string InstrumentSymbol { get; set; } = string.Empty;
            public OrderSide Side { get; set; }
            public int Quantity { get; set; }
            public decimal? LimitPrice { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult<Order>> PlaceOrder([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
        {
            if (request == null) return BadRequest();

            try
            {
                var order = await _orderService.PlaceOrderAsync(
                    request.CustomerId,
                    request.AccountId,
                    request.InstrumentSymbol,
                    request.Side,
                    request.Quantity,
                    request.LimitPrice,
                    cancellationToken).ConfigureAwait(false);

                return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInformation(ex, "Failed to place order");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{orderId:guid}/cancel")]
        public async Task<ActionResult> CancelOrder(Guid orderId, CancellationToken cancellationToken)
        {
            var ok = await _orderService.CancelOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
            if (!ok) return NotFound();
            return Ok();
        }

        [HttpGet("{orderId:guid}")]
        public async Task<ActionResult<Order>> GetOrder(Guid orderId, CancellationToken cancellationToken)
        {
            var order = await _orderService.GetOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Order>>> ListOrders([FromQuery] Guid? customerId, [FromQuery] Guid? accountId, CancellationToken cancellationToken)
        {
            var orders = await _orderService.ListOrdersAsync(customerId, accountId, cancellationToken).ConfigureAwait(false);
            return Ok(orders);
        }

        [HttpPost("{orderId:guid}/execute")]
        public async Task<ActionResult<IReadOnlyList<Trade>>> ExecuteOrder(Guid orderId, CancellationToken cancellationToken)
        {
            try
            {
                var trades = await _orderService.ExecuteOrderAsync(orderId, cancellationToken).ConfigureAwait(false);
                return Ok(trades);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogInformation(ex, "Failed to execute order {OrderId}", orderId);
                return BadRequest(ex.Message);
            }
        }
    }
}