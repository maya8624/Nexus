using Nexus.Application.Dtos;
using Nexus.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Factories
{
    public interface IAuthServiceFactory
    {
        IAuthService GetAuthProvider(string provider);
    }
}
    