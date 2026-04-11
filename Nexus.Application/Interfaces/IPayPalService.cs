using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Interfaces
{
    public interface IPayPalService
    {
        Task<PayPalOrderResultResponse?> CreateOrder(int orderId, CancellationToken ct);
        Task<PayPalCaptureResponse?> CaptureOrder(int orderId, CancellationToken ct);
        Task<PayPalRefundResponse> RefundCapture(int paymentId, decimal amount, CancellationToken ct);
    }
}
