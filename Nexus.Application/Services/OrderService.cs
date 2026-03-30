using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Domain.Entities;
using Nexus.Application.ReadModels;
using Nexus.Application.Interfaces.Repository;
using Nexus.Application.Interfaces.Business;

namespace Nexus.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _uow;

        public OrderService(IOrderRepository orderRepository, IUnitOfWork uow)
        {
            _orderRepository = orderRepository;
            _uow = uow;
        }

        public async Task<OrderResponse?> GetOrderById(int orderId)
        {
            var order = await _orderRepository.GetOrderById(orderId);

            if (order == null)
                return null;

            var response = new OrderResponse
            {
                OrderId = order.Id,
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Items = order.Items.Select(x => new OrderItemResponse
                {
                    ProductId = x.ProductId, 
                    ProductName = x.ProductName,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    TotalPrice = x.TotalPrice,
                }).ToList()
            };

            return response;
        }

        public async Task<IEnumerable<OrderSummaryReadModel>> GetOrdersForUser(int userId)
        {
            var orders = await _orderRepository.GetOrdersForUser(userId);
            return orders;
        }
        
        public async Task<OrderResponse> CreateOrder(int userId, string frontendIdempotencyKey, List<CreateOrderItemRequest> items)
        {
            var existing = await _orderRepository
                .GetOrderByFrontendIdempontentKey(frontendIdempotencyKey, userId);

             if (existing != null)
                return MapToOrderResponse(existing);

            var orderItems = items.Select(x => new OrderItem
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice
            }).ToList();

            var totalAmount = items.Sum(x => x.Quantity * x.UnitPrice);
            
            var order = new Order
            {
                UserId = userId,
                FrontendIdempotencyKey = frontendIdempotencyKey,
                TotalAmount = totalAmount,
                Items = orderItems
            };

            await _orderRepository.Create(order, CancellationToken.None);
            await _uow.SaveChanges();

            return MapToOrderResponse(order);
        }

        // Deletes an order by its ID.
        // Returns true if the order was found and deleted, false otherwise.
        public async Task<bool> DeleteOrder(int orderId)
        {
            var order = await _orderRepository.Find(orderId, CancellationToken.None);
            if (order == null) 
                return false;
            
            _orderRepository.Delete(order);
            await _uow.SaveChanges();

            return true;
        }

        private static OrderResponse MapToOrderResponse(Order order)
        {
            return new OrderResponse
            {
                OrderId = order.Id,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                IdempotencyKey = order.FrontendIdempotencyKey,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(x => new OrderItemResponse
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Quantity = x.Quantity,
                    UnitPrice = x.UnitPrice,
                    TotalPrice = x.Quantity * x.UnitPrice
                }).ToList()
            };
        }
    }
}
