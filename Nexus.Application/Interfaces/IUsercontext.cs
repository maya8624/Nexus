using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Application.Interfaces
{
    public interface IUserContext
    {
        string? UserId { get; }
        string? Email { get; }
        bool IsAuthenticated { get; }
    }
}
