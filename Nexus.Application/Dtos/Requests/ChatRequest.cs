using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Dtos.Requests
{
    public class ChatRequest
    {
        public required string Message { get; set; }
        public required string ThreadId { get; set; }
        public string? PropertyId { get; set; }
    }

    public class ChatRequestValidator : AbstractValidator<ChatRequest>
    {
        public ChatRequestValidator()
        {
            RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.ThreadId).NotEmpty();
        }
    }
}
