using System.Linq;
using System.Net;

namespace RCON.Utils
{
    internal static class IpExtension
    {
        public static bool Match(string pattern, string ipAddress)
        {
            var subnetMask = IPAddress
                .Parse(string.Join(".", pattern.Split('.').Select(s => s == "*" ? "0" : "255").ToArray()))
                .GetAddressBytes();
            var patternIp = IPAddress.Parse(pattern.Replace('*', '0')).GetAddressBytes();
            var ip = IPAddress.Parse(ipAddress).GetAddressBytes();

            return subnetMask.Where((t, i) => (t & patternIp[i]) != (t & ip[i])).Any() == false;
        }
    }
}