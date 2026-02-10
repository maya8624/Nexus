using NexusPay.Application.Dtos;
using NexusPay.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusPay.Application.Factories
{
    public interface IAuthServiceFactory
    {
        IAuthService GetAuthProvider(string provider);
    }
}
    