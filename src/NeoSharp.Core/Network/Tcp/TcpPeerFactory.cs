﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NeoSharp.Core.Network.Tcp
{
    public class TcpPeerFactory : ITcpPeerFactory
    {
        private readonly bool _forceIPv6;
        private readonly ILogger<TcpPeerFactory> _logger;
        private readonly ILogger<TcpPeer> _peerLogger;

        public TcpPeerFactory(NetworkConfig config, ILogger<TcpPeerFactory> logger, ILogger<TcpPeer> peerLogger)
        {
            _forceIPv6 = config.ForceIPv6;
            _logger = logger;
            _peerLogger = peerLogger;
        }

        public async Task<IPeer> Create(EndPoint endPoint)
        {
            var ipAddress = await GetIpAddress(endPoint.Host);
            if (ipAddress == null)
            {
                throw new InvalidOperationException($"\"{endPoint.Host}\" cannot be resolved to an ip address.");
            }

            ipAddress = _forceIPv6 ? ipAddress.MapToIPv6() : ipAddress;

            var ipEp = new IPEndPoint(ipAddress, endPoint.Port);

            _logger.LogInformation($"Connecting to {ipEp}");

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(ipEp.Address, ipEp.Port); // TODO: thread etc

            _logger.LogInformation($"Connected to {ipEp}");

            return Create(socket);
        }

        private static async Task<IPAddress> GetIpAddress(string hostNameOrAddress)
        {
            if (IPAddress.TryParse(hostNameOrAddress, out var ipAddress))
            {
                return ipAddress;
            }

            IPHostEntry ipHostEntry;

            try
            {
                ipHostEntry = await Dns.GetHostEntryAsync(hostNameOrAddress);
            }
            catch (SocketException)
            {
                return null;
            }

            return ipHostEntry.AddressList
                .FirstOrDefault(p => p.AddressFamily == AddressFamily.InterNetwork || p.IsIPv6Teredo);
        }

        public TcpPeer Create(Socket socket)
        {
            return new TcpPeer(socket, _peerLogger);
        }
    }
}