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
        public required string SessionId { get; set; }
    }

    public class ChatRequestValidator : AbstractValidator<ChatRequest>
    {
        public ChatRequestValidator()
        {
            RuleFor(x => x.Message).NotEmpty();
            RuleFor(x => x.SessionId).NotEmpty();
        }
    }
}
