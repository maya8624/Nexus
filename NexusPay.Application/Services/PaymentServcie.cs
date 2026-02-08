using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexusPay.Application.Exceptions;
using NexusPay.Application.Interfaces;
using NexusPay.Domain.Enums;
using NexusPay.Infrastructure.Interfaces;
using NexusPay.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusPay.Application.Services
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
