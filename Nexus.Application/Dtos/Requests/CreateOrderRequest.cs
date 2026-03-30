using FluentValidation;
using Nexus.Domain.Enums;

namespace Nexus.Application.Dtos.Requests
{
    public class CreateOrderRequest
    {
        public int UserId { get; set; }
        public string FrontendIdempotencyKey { get; set; }
        public List<CreateOrderItemRequest> Items { get; set; } = [];
    }

    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().GreaterThan(0);
            RuleFor(x => x.FrontendIdempotencyKey).NotEmpty();
            RuleFor(x => x.Items).NotEmpty();

            RuleForEach(x => x.Items).ChildRules(items =>
            {
                items.RuleFor(i => i.ProductId).NotEmpty().GreaterThan(0);
                items.RuleFor(i => i.ProductName).NotEmpty();
                items.RuleFor(i => i.Quantity).GreaterThan(0);
                items.RuleFor(i => i.UnitPrice).GreaterThan(0);
            });
        }
    }
}
