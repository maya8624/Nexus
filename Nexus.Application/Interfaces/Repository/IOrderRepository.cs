using Nexus.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nexus.Application.ReadModels;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IOrderRepository : IRepositoryBase<Order>
    {
        Task<Order?> GetOrderById(int orderId);
        Task<OrderForPaymentReadModel> GetOrderForPayment(int orderId);
        Task<IEnumerable<OrderSummaryReadModel>> GetOrdersForUser(int userId);
        Task<Order> GetOrderByFrontendIdempontentKey(string key, int userId);
    }
}
