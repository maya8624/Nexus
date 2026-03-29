using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos;
using Nexus.Api.Controllers;
using Nexus.Application.ReadModels;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Controllers
{
    public class OrderController : AppControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("{orderId:int}")]
        public async Task<ActionResult<OrderResponse>> GetOrder(int orderId)
        {
            var order = await _orderService.GetOrderById(orderId);

            if (order == null)
                return NotFound();

            return Ok(order);
        }

        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<OrderSummaryReadModel>>> GetOrders()
        {
            //TODO: get userId from JWT claims
            //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //if (userId == null) return Unauthorized();
            //userId = 1 for testing
            var userId = 1;
            var orders = await _orderService.GetOrdersForUser(userId);

            return Ok(orders);
        }

        [HttpPost("create")]
        public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request)
        {
            var order = await _orderService.CreateOrder(
                request.UserId,
                request.FrontendIdempotencyKey,
                request.Items);

            return Ok(order);
        }

        [HttpDelete("{orderId:int}")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var result = await _orderService.DeleteOrder(orderId);
            
            if (result == false)
                return NotFound();

            return NoContent();
        }
    }
}
