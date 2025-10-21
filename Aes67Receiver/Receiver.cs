using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace Aes67Receiver
{
    /// <summary>
    /// Receives AES67 RTP multicast audio streams and exposes received samples.
    /// </summary>
    public class Receiver
    {
        /// <summary>
        /// Logger instance for diagnostic output.
        /// </summary>
        private readonly ILogger logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Receiver>();

        /// <summary>
        /// Event triggered when audio samples are received and processed.
        /// </summary>
        public event Action<byte[]>? SamplesReceived;

        /// <summary>
        /// Multicast IP address to listen for AES67 streams.
        /// </summary>
        private readonly IPAddress ipAddress;

        /// <summary>
        /// UDP port to listen for AES67 streams.
        /// </summary>
        private readonly int port;

        /// <summary>
        /// List of parsed SDP information for received streams.
        /// </summary>
        private readonly List<SdpInfo> sdpInfos = new();

        /// <summary>
        /// UDP client for receiving RTP packets.
        /// </summary>
        private readonly UdpClient udpClient;

        /// <summary>
        /// Cancellation token for stopping the receiver.
        /// </summary>
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="Receiver"/> class.
        /// </summary>
        /// <param name="cancellationToken">Token to signal receiver shutdown.</param>
        /// <param name="ipAddress">Multicast IP address to join.</param>
        /// <param name="port">UDP port to bind (default: 5004).</param>
        public Receiver(CancellationToken cancellationToken, IPAddress ipAddress, int port = 5004)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            this.cancellationToken = cancellationToken;

            logger.LogInformation("Initializing AES67 Receiver for {IPAddress}:{Port}", ipAddress, port);

            udpClient = new UdpClient(AddressFamily.InterNetwork);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            udpClient.JoinMulticastGroup(ipAddress);
        }

        /// <summary>
        /// Starts listening for AES67 RTP packets and processes incoming audio samples.
        /// </summary>
        public async void Start()
        {
            logger.LogInformation("Listening for AES67 RTP stream on {IPAddress}:{Port}...", ipAddress, port);

            while (!cancellationToken.IsCancellationRequested)
            {
                var data = await udpClient.ReceiveAsync().ConfigureAwait(false);
                var dataProcessed = ProcessRtpPacket(data.Buffer);

                SamplesReceived?.Invoke(dataProcessed);
            }

            logger.LogWarning("Receiver stopped.");
        }

        /// <summary>
        /// Processes an RTP packet, extracting and converting the audio payload.
        /// </summary>
        /// <param name="packet">Raw RTP packet bytes.</param>
        /// <returns>Processed audio sample bytes, or empty array if invalid.</returns>
        private byte[] ProcessRtpPacket(byte[] packet)
        {
            const int headerLength = 12;
            if (packet.Length < headerLength)
                return Array.Empty<byte>();

            // Avoid extra allocations: use Span for payload extraction
            var payload = packet.AsSpan(headerLength);

            // Parse header directly from packet
            var rtpHeader = RtpHeader.Parse(packet[..headerLength]);

            // AES67: L24 big endian → little endian
            return Convert24BitBigEndianToLittleEndian(payload);
        }

        /// <summary>
        /// Converts 24-bit big-endian audio samples to little-endian format.
        /// </summary>
        /// <param name="data">Audio payload as a span of bytes.</param>
        /// <returns>Converted audio sample bytes.</returns>
        private static byte[] Convert24BitBigEndianToLittleEndian(ReadOnlySpan<byte> data)
        {
            int len = data.Length - (data.Length % 3);
            byte[] result = new byte[len];
            for (int i = 0; i < len; i += 3)
            {
                // 24-bit swap (big → little)
                result[i] = data[i + 2];
                result[i + 1] = data[i + 1];
                result[i + 2] = data[i];
            }
            return result;
        }
    }
}
