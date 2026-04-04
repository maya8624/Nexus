using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nexus.Application.Dtos.Requests;
using Nexus.Application.Dtos.Responses;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Settings;
using Stripe;
using Stripe.Checkout;

namespace Nexus.Api.Controllers
{
    [Route("api/deposits")]
    public class DepositsController : AppControllerBase
    {
        private readonly IDepositService _depositService;
        private readonly StripeSettings _stripeSettings;

        public DepositsController(IDepositService depositService, IOptions<StripeSettings> stripeSettings)
        {
            _depositService = depositService;
            _stripeSettings = stripeSettings.Value;
        }

        [HttpPost("checkout")]
        public async Task<ActionResult<DepositResponse>> CreateCheckoutSession([FromBody] CreateDepositRequest request, CancellationToken ct)
        {
            var result = await _depositService.CreateCheckoutSessionAsync(request, ct);
            if (result.IsSuccess)
                return Ok(result.Value);

            return MapFailure(result);
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook(CancellationToken ct)
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync(ct);
            var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    payload, stripeSignature, _stripeSettings.WebhookSecret);

                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var session = (Session)stripeEvent.Data.Object;
                    await _depositService.FulfillDepositAsync(session.Id, ct);
                }
            }
            catch (StripeException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok();
        }
    }
}
