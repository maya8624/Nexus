using Nexus.Application.Dtos;
using Nexus.Application.ReadModels;

namespace Nexus.Application.Interfaces.Business
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrder(int userId, string frontendIdempotencyKey, List<CreateOrderItemRequest> items);
        Task<bool> DeleteOrder(int orderId);
        Task<OrderResponse?> GetOrderById(int orderId);
        Task<IEnumerable<OrderSummaryReadModel>> GetOrdersForUser(int userId);
    }
}
