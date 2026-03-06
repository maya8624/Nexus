using Nexus.Application.Constants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nexus.Application.Exceptions
{
    public class UserException : AppException
    {
        public override int StatusCode => CustomStatusCodes.ExistIssue;
        public override string Name => "EXIST_ISSUE";

        public UserException(string message) : base(message)
        {
        }
    }
}
