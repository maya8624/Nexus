using Nexus.Domain.Entities;
using Nexus.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IPaymentRepository : IRepositoryBase<Payment>
    {
        Task<Payment?> GetByOrderId(int orderId);
        Task<Payment?> GetPending(int orderId, string providerOrderId, PaymentStatus status);
        Task<Payment?> GetByProviderOrderId(string providerOrderId);
    }
}
