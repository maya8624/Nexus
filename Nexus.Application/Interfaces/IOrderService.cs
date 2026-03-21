using Nexus.Application.Dtos;
using Nexus.Infrastructure.Responses;

namespace Nexus.Application.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrder(int userId, string frontendIdempotencyKey, List<CreateOrderItemRequest> items);
        Task<bool> DeleteOrder(int orderId);
        Task<OrderResponse?> GetOrderById(int orderId);
        Task<IEnumerable<OrderSummaryReadModel>> GetOrdersForUser(int userId);
    }
}
