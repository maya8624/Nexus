using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Network.Interfaces
{
    public interface IHttpClientService
    {
        Task<T> ExecuteRequest<T>(HttpRequestMessage request);
        Task ExecuteRequest(HttpRequestMessage request, CancellationToken ct);
    }
}
