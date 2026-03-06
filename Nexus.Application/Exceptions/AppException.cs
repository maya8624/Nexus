using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Exceptions
{
    public abstract class AppException : Exception
    {
        public AppException(string message) : base(message) { }
        public AppException(string message, Exception inner) : base(message, inner) { }

        public abstract int StatusCode { get; }
        public abstract string Name { get; }
    }
}
