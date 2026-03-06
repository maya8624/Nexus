using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos
{
    public class OrderPaymentRequest
    {
        public int OrderId { get; set; }
    }

    public class OrderPaymentRequestValidator : AbstractValidator<OrderPaymentRequest>
    {
        public OrderPaymentRequestValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
        }
    }
}
