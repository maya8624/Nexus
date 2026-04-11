using Microsoft.AspNetCore.Mvc;
using Nexus.Application.Dtos;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces;

namespace Nexus.Api.Controllers
{
    public class PayPalController : AppControllerBase
    {
        private readonly IPayPalService _paypalService;

        public PayPalController(IPayPalService paypalService)
        {
            _paypalService = paypalService;
        }

        [HttpPost("create-order")]
        public async Task<ActionResult<PayPalOrderResultResponse>> CreatePayPalOrder([FromBody] OrderPaymentRequest request, CancellationToken ct)
        {
            var result = await _paypalService.CreateOrder(request.OrderId, ct);
            return Ok(result);
        }

        [HttpPost("capture-order")]
        public async Task<ActionResult<PayPalCaptureResponse>> CaptureOrder([FromBody] OrderPaymentRequest request, CancellationToken ct)
        {
            var result = await _paypalService.CaptureOrder(request.OrderId, ct);

            if (result == null)
                return NotFound();
           
            return Ok(result);
        }

        [HttpPost("refund")]
        public async Task<ActionResult<PayPalRefundResponse>> Refund(RefundRequest request, CancellationToken ct)
        {
            var refund = await _paypalService.RefundCapture(request.PaymentId, request.Amount, ct);
            return refund;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook(int orderId)
        {
            return Ok(); // Always return 200 quickly to acknowledge receipt
        }
    }
}
