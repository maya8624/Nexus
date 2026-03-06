using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexus.Application.Dtos;
using Nexus.Application.Exceptions;
using Nexus.Application.Interfaces;
using Nexus.Domain.Enums;
using Nexus.Infrastructure.Responses;
using Nexus.Network;
using Nexus.Network.Enums;
using Nexus.Network.Extensions;
using Nexus.Network.Interfaces;
using Nexus.Network.Services;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Interfaces;
using System.Text.Json;
using Nexus.Application.Extensions;
using System.Net.Http.Json;

namespace Nexus.Application.Services
{
    public class PayPalService : IPayPalService
    {
        private readonly IPayPalAuthService _authService;
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<PayPalAuthService> _logger;
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IRefundRepository _refundRepository;
        private readonly PayPalSettings _settings;
        private readonly IUnitOfWork _uow;

        public PayPalService(
            IPayPalAuthService authService,
            ILogger<PayPalAuthService> logger,
            IOrderRepository orderRepository,
            IPaymentRepository paymentRepository,
            IOptions<PayPalSettings> settings,
            IHttpClientService httpClientService,
            IRefundRepository refundRepository,
            IUnitOfWork uow)
        {
            _authService = authService;
            _logger = logger;
            _orderRepository = orderRepository;
            _paymentRepository = paymentRepository;
            _settings = settings.Value;
            _httpClientService = httpClientService;
            _refundRepository = refundRepository;
            _uow = uow;
        }

        public async Task<PayPalOrderResultResponse> CreateOrder(int orderId)
        {
            var order = await _orderRepository.GetOrderForPayment(orderId);
            if (order == null)
                throw new NotFoundException($"Order: {orderId} not found.");

            var payment = await GetPaymentByOrderId(orderId);
            if (payment == null)
            {
                var idempotencyKey = Guid.NewGuid().ToString();
                payment = await SavePaymentRecord(order, idempotencyKey);
            }

            var accessToken = await _authService.GetAccessToken();
            var request = BuildCreateRequest(order, payment.BackendIdempotencyKey, accessToken);
            var result = await _httpClientService.ExecuteRequest<PayPalOrderResponse>(request);
            
            var approveUrl = ValidatePayPalCreateOrderResult(result);
            await UpdatePayment(payment, result.Id);

            var response = new PayPalOrderResultResponse
            {
                PayPalOrderId = result.Id,
                ApproveUrl = approveUrl,
            };

            return response;
        }        

        private async Task<Payment> GetPaymentByOrderId(int orderId)
        { 
            var payment = await _paymentRepository.GetByOrderId(orderId);

            if (payment != null && payment.Status != PaymentStatus.Pending)
                throw new Exception($"Order status {orderId} is not Pending.");

            return payment;
        }

        public async Task<PayPalCaptureResponse> CaptureOrder(int orderId)
        {
            var payment = await _paymentRepository.GetByOrderId(orderId);
            if (payment == null)
                throw new NotFoundException($"Payment not found. orderId:{orderId}");

            if (payment.Status == PaymentStatus.Completed && string.IsNullOrEmpty(payment.RawResponse) == false)
                return payment.RawResponse.SafeDeserialize<PayPalCaptureResponse>();

            var accessToken = await _authService.GetAccessToken();
            var request = BuildCaptureRequest(payment.ProviderOrderId, payment.BackendIdempotencyKey, accessToken);
            var response = await _httpClientService.ExecuteRequest<PayPalCaptureResponse>(request);

            await UpdateCaptureResult(payment, response);
            return response;
        }

        public async Task<PayPalRefundResponse> RefundCapture(int paymentId, decimal amount, CancellationToken ct)
        {
            var payment = await _paymentRepository.Find(paymentId);
            ValidatePaymentForRefund(payment, amount);

            var idempotencyKey = Guid.NewGuid().ToString();
            var accessToken = await _authService.GetAccessToken();
            var options = BuildRefundRequest(payment, amount, idempotencyKey, accessToken);

            var request = HttpRequestFactory.CreateHttpRequestMessage(options);
            var response = await _httpClientService.ExecuteRequest<PayPalRefundResponse>(request);

            var refund = CreateRefundRecord(payment, amount, response, idempotencyKey);
            await _refundRepository.Create(refund);

            UpdateRefundPayment(payment, amount);
            _paymentRepository.Update(payment);
            await _uow.SaveChanges();

            return response;
        }

        #region private methods
        private string ValidatePayPalCreateOrderResult(PayPalOrderResponse result)
        {
            if (result == null || result.Id == null)
            {
                _logger.LogError("Failed to get PayPalOrderId.");
                throw new PayPalException("Failed to get PayPalOrderId.");
            }

            var approveUrl = result.Links?.FirstOrDefault(x => x.Rel == "approved")?.Href;

            if (approveUrl == null)
            {
                _logger.LogError("Failed to get an approve url.");
                throw new PayPalException("Failed to get an approve url.");
            }

            return approveUrl;
        }

        private static void ValidatePaymentForRefund(Payment payment, decimal amount)
        {
            if (payment == null)
                throw new NotFoundException("Payment not found.");

            if (amount <= 0 || amount > payment.RefundableAmount)
                throw new RefundException("Invalid refund amount.");

            if (string.IsNullOrEmpty(payment.ProviderCaptureId))
                throw new PaymentException("Payment has not been captured yet.");
        }

        private RequestBuilderOptions BuildRefundRequest(Payment payment, decimal amount, string idempotencyKey, string accessToken)
        {
            var captureUrl = string.Format(_settings.SandboxRefundCaptureUrl, payment.ProviderCaptureId);
            var url = _settings.SandboxBaseUrl.CombineUrl(captureUrl);

            var body = JsonContent.Create(new
            {
                amount = new
                {
                    value = amount.ToString("F2"),
                    currency_code = Currency.AUD.ToString(),
                }
            });

            var headers = new Dictionary<string, string>
            {
                ["PayPal-Request-Id"] = idempotencyKey
            };

            var options = new RequestBuilderOptions
            {
                Method = HttpMethod.Post,
                AuthScheme = AuthScheme.Bearer,
                AuthToken = accessToken,
                Body = body,
                Headers = headers,
                Url = url
            };

            return options;
        }

        private static Refund CreateRefundRecord(Payment payment, decimal amount, PayPalRefundResponse response, string idempotencyKey)
        {
            return new Refund
            {
                PaymentId = payment.Id,
                Provider = payment.Provider,
                ProviderRefundId = response.Id,
                Amount = amount,
                Status = RefundStatus.Completed,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                BackendIdempotencyKey = idempotencyKey,
                RawResponse = response.Raw
            };
        }

        private static void UpdateRefundPayment(Payment payment, decimal amount)
        {
            payment.RefundedAmount += amount;
            payment.Status = payment.RefundableAmount == 0
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyRefunded;
        }

        //TODO: split into small methods
        private async Task UpdateCaptureResult(Payment payment, PayPalCaptureResponse response)
        {
            var capture = response
                ?.PurchaseUnits.FirstOrDefault()
                ?.PaymentDetails
                ?.Captures.FirstOrDefault();

            if (capture == null)
                throw new NotFoundException("capture is not found");

            payment.Status = capture.Status switch
            {
                "COMPLETED" => PaymentStatus.Completed,
                "PENDING" => PaymentStatus.Pending,
                _ => PaymentStatus.Failed
            };

            payment.ProviderCaptureId = capture.Id;
            payment.CapturedAmount = decimal.Parse(capture.Amount.Value);
            payment.CapturedAt = DateTimeOffset.UtcNow;
            payment.RawResponse = JsonSerializer.Serialize(response);

            _paymentRepository.Update(payment);
            await _uow.SaveChanges();
        }
   
        //private static async Task<HttpResponseMessage> SendWithRetry(
        //    HttpClient client,
        //    HttpRequestMessage request,
        //    CancellationToken ct)
        //{
        //    const int maxRetries = 3;

        //    for (var attempt = 1; attempt <= maxRetries; attempt++)
        //    {
        //        var response = await client.SendAsync(request, ct);

        //        if (response.IsSuccessStatusCode)
        //            return response;

        //        if (attempt == maxRetries)
        //            response.EnsureSuccessStatusCode();

        //        await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
        //    }

        //    throw new InvalidOperationException("Retry loop exited unexpectedly");
        //}


        private HttpRequestMessage BuildCaptureRequest(string paypalOrderId, string idempotencyKey, string accessToken)
        {
            var captureUrl = string.Format(_settings.SandboxCaptureOrderUrl, paypalOrderId);
            var url = _settings.SandboxBaseUrl.CombineUrl(captureUrl);
            var headers = new Dictionary<string, string>
            {
                ["PayPal-Request-Id"] = idempotencyKey
            };

            var options = new RequestBuilderOptions
            {
                Method = HttpMethod.Post,
                AuthToken = accessToken,
                AuthScheme = AuthScheme.Bearer,
                Headers = headers,
                Url = url
            };

            var request = HttpRequestFactory.CreateHttpRequestMessage(options);
            return request;
        }

        private HttpRequestMessage BuildCreateRequest(OrderForPaymentResponse order, string idempotencyKey, string accessToken)
        {
            var url = _settings.SandboxBaseUrl.CombineUrl(_settings.SandboxCreateOrderUrl);
            var body = BuildPayPalOrderRequest(order);
            var headers = new Dictionary<string, string>
            {
                ["PayPal-Request-Id"] = idempotencyKey
            };

            var options = new RequestBuilderOptions
            {
                Method = HttpMethod.Post,
                AuthScheme = AuthScheme.Bearer,
                Body = body,
                AuthToken = accessToken,
                Headers = headers,
                Url = url
            };

            var request = HttpRequestFactory.CreateHttpRequestMessage(options);
            return request;
        }

        private object BuildPayPalOrderRequest(OrderForPaymentResponse order)
        {
            return new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        reference_id = order.Id.ToString(),
                        amount = new
                        {
                            currency_code = order.Currency.ToString(),
                            value = order.TotalAmount.ToString("F2")
                        },
                        //items = order.Items.Select(i => new
                        //{
                        //    name = i.ProductName,
                        //    unit_amount = new
                        //    {
                        //        currency_code = order.Currency.ToString(),
                        //        value = i.UnitPrice.ToString("F2")
                        //    },
                        //    quantity = i.Quantity.ToString()
                        //}).ToArray()
                    }
                }
            };
        }

        private async Task<Payment> SavePaymentRecord(OrderForPaymentResponse order, string backendIdempotencyKey)
        {
            try
            {
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Provider = PaymentProvider.PayPal,
                    Status = PaymentStatus.Pending,
                    Amount = order.TotalAmount,
                    Currency = order.Currency,
                    BackendIdempotencyKey = backendIdempotencyKey,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                await _paymentRepository.Create(payment);
                await _uow.SaveChanges();

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save Payment record for Order {OrderId}", order.Id);
                throw new PaymentException("Could not save payment record. Please try again.");
            }
        }

        private async Task UpdatePayment(Payment payment, string paypalOrderId)
        {
            await _uow.SaveChanges();
            try
            {
                payment.ProviderOrderId = paypalOrderId;
                payment.UpdatedAt = DateTimeOffset.UtcNow;

                _paymentRepository.Update(payment);
                await _uow.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update Payment record for Order {OrderId}", payment.OrderId);
                throw new PaymentException("Failed to process payment. Please try again.");
            }
        }
        #endregion
    }
}
