using Nexus.Application.Interfaces;

namespace Nexus.Application.Factories
{
    public class AuthServiceFactory : IAuthServiceFactory
    {
        private readonly IEnumerable<IAuthService> _authServices;

        public AuthServiceFactory(IEnumerable<IAuthService> authServices)
        {
            _authServices = authServices;
        }

        public IAuthService GetAuthProvider(string provider)
        {
            var service = _authServices.FirstOrDefault(x => x.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

            //If not found, throw exception
            return service ?? throw new NotSupportedException($"Provider '{provider}' is not supported.");
        }
    }
}
