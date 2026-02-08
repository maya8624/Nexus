using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusPay.Application
{
    public class JwtSettings
    {
        public string Key { get; }
        public string Issuer { get; }
        public string Audience { get; }
        public string CookieName { get; }
    }
}
