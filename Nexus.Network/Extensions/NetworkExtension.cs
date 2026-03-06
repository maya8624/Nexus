using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus.Network.Extensions
{
    public static class NetworkExtension
    {
        public static string CombineUrl(this string baseUrl, string subUrl)
        {
            // TODO: apply a custom exception
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentNullException(nameof(baseUrl));

            if (string.IsNullOrWhiteSpace(subUrl))
                return baseUrl;

            return new Uri(new Uri(baseUrl), subUrl).ToString();
        }
    }
}
