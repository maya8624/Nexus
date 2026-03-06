using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos;
using Nexus.Infrastructure.Responses;
using Nexus.Application.Interfaces;
using System.Security.Claims;
using Nexus.Api.Controllers;

namespace Nexus.Controllers
{
    public class OrderController : NexusPayControllerBase
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
            return Ok(order);
        }

        [HttpGet("orders")]
        public async Task<ActionResult<List<OrderSummaryResponse>>> GetOrders()
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
