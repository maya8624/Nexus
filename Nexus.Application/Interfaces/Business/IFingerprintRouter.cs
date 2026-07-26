using Nexus.Domain.Entities;

namespace Nexus.Application.Interfaces.Business
{
    public interface IFingerprintRouter
    {
        string Route(Fingerprint fingerprint);
    }
}
