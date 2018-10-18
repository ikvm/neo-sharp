using System.Net;

namespace NeoSharp.Core.Extensions
{
    public static class IPAddressExtensions
    {
        public static IPAddress MapToIPv6OrIPv4(this IPAddress ipAddress, bool forceIPv6)
        {
            if (forceIPv6)
            {
                return ipAddress.MapToIPv6();
            }
            else if(ipAddress.IsIPv4MappedToIPv6)
            {
                return ipAddress.MapToIPv4();
            }

            return ipAddress;
        }
    }
}