using Microsoft.EntityFrameworkCore;
using Nexus.Infrastructure.Responses;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Interfaces;
using Nexus.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Infrastructure.Repositories
{
    public class OrderRepository : RepositoryBase<Order>, IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context) : base(context) 
        {
            _context = context;
        }
        
        public async Task<Order?> GetOrderByFrontendIdempontentKey(string key, int userId)
        { 
            return await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FrontendIdempotencyKey == key && x.UserId == userId);
        }

        public async Task<Order?> GetOrderById(int orderId)
        {
            return await _context.Orders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == orderId);
        }

        public async Task<IEnumerable<OrderSummaryResponse>> GetOrdersForUser(int userId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new OrderSummaryResponse
                {
                    OrderId = x.Id,
                    Status = x.Status.ToString(),
                    TotalAmount = x.TotalAmount,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<OrderForPaymentResponse?> GetOrderForPayment(int orderId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new OrderForPaymentResponse
                {
                    Id = o.Id,
                    TotalAmount = o.TotalAmount,
                    Currency = o.Currency,
                    FrontendIdempotencyKey = o.FrontendIdempotencyKey,
                    Items = o.Items.Select(i => new OrderItemForPaymentResponse
                    {
                        ProductName = i.ProductName,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }
    }
}
