using Microsoft.Extensions.Logging;
using Nexus.Application.Interfaces.Business;
using Nexus.Application.Interfaces.Repository;

namespace Nexus.Application.Services
{
    public class PaymentServcie : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<PaymentServcie> _logger;
        private readonly IUnitOfWork _uow;

        public PaymentServcie(IPaymentRepository paymentRepository, ILogger<PaymentServcie> logger, IUnitOfWork uow)
        {
            _paymentRepository = paymentRepository;
            _logger = logger;
            _uow = uow;
        }

        //public async Task ProcessPayment(decimal amount)
        //{
        //    if (!userContext.IsAuthenticated)
        //        throw new UnauthorizedAccessException();

        //    var userId = userContext.UserId;

        //    // Now you can easily save the payment linked to this specific user!
        //    await repo.SaveTransactionAsync(userId, amount);
        //}
    }
}
