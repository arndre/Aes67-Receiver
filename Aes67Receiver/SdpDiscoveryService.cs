using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Aes67Receiver
{
    /// <summary>
    /// Provides discovery of AES67 SDP (Session Description Protocol) streams via SAP multicast.
    /// </summary>
    public class SdpDiscoveryService
    {
        /// <summary>
        /// The cancellation token used to stop the discovery service.
        /// </summary>
        private CancellationToken ct;

        /// <summary>
        /// Stores discovered SDP information, keyed by session name.
        /// </summary>
        private ConcurrentDictionary<string, SdpInfo> discoveredSdpInfos = new ConcurrentDictionary<string, SdpInfo>();

        /// <summary>
        /// Gets the collection of currently active discovered SDP infos.
        /// </summary>
        public IEnumerable<SdpInfo> DiscoveredSdpInfos => discoveredSdpInfos.Values.Where(p => p.Active);

        /// <summary>
        /// The logger instance for diagnostic output.
        /// </summary>
        private new readonly ILogger logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<SdpDiscoveryService>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SdpDiscoveryService"/> class.
        /// </summary>
        /// <param name="ct">A cancellation token to control the lifetime of the service.</param>
        public SdpDiscoveryService(CancellationToken ct)
        {
            this.ct = ct;
        }

        /// <summary>
        /// Starts the SAP multicast listener to discover SDP streams.
        /// </summary>
        /// <param name="multicastAddress">The multicast address to listen on.</param>
        /// <param name="port">The port to listen on (default is 9875).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task Run(IPAddress multicastAddress, int port = 9875)
        {
            // Listening Task
            _ = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    string? sdpText = await SapListenOnce(multicastAddress, port);
                    if (!string.IsNullOrEmpty(sdpText))
                    {
                        SdpInfo sdpInfo = SdpInfo.Parse(sdpText);

                        if (!discoveredSdpInfos.ContainsKey(sdpInfo.SessionName))
                        {
                            logger.LogInformation("SAP Discovery - SDP Info add:" + sdpInfo.SessionName);
                        }
                        // Update or add
                        discoveredSdpInfos[sdpInfo.SessionName] = sdpInfo;
                    }
                }
            }, ct);
        }

        /// <summary>
        /// Listens for a single SAP packet and extracts the SDP text.
        /// </summary>
        /// <param name="multicastAddress">The multicast address to join.</param>
        /// <param name="port">The port to listen on.</param>
        /// <returns>
        /// A task that returns the SDP text if found, or an empty string if not.
        /// </returns>
        public async Task<string> SapListenOnce(IPAddress multicastAddress, int port)
        {
            using var client = new UdpClient(AddressFamily.InterNetwork);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, port));

            // Join Multicast
            client.JoinMulticastGroup(multicastAddress);

            var result = await client.ReceiveAsync();
            string text = Encoding.UTF8.GetString(result.Buffer);

            // SAP header may precede SDP, so search for "v="
            int idx = text.IndexOf("v=");
            return idx >= 0 ? text.Substring(idx) : string.Empty;
        }
    }
}
