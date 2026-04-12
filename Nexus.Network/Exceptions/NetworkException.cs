
namespace Nexus.Application.Exceptions
{
    public abstract class NetworkException : Exception
    {
        public abstract int StatusCode { get; }
        public abstract int HttpStatusCode { get; }
        public abstract string Name { get; }
     
        public NetworkException(string message) : base(message) { }
        public NetworkException(string message, Exception inner) : base(message, inner) { }

    }
}
