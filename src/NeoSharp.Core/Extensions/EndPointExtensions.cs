using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NeoSharp.Core.Network;

namespace NeoSharp.Core.Extensions
{
    public static class EndPointExtensions
    {
        public static async Task<IPAddress> GetIPAddress(this Network.EndPoint endPoint)
        {
            if (IPAddress.TryParse(endPoint.Host, out var ipAddress))
            {
                return ipAddress;
            }

            try
            {
                var ipHostEntry = await Dns.GetHostEntryAsync(endPoint.Host);

                return ipHostEntry.AddressList
                    .FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork || p.IsIPv6Teredo);
            }
            catch
            {
                return null;
            }
        }
    }
}