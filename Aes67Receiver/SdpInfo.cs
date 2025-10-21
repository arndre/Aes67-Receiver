using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Aes67Receiver
{
    /// <summary>
    /// Supported audio codecs for SDP parsing.
    /// </summary>
    public enum AudioCodec
    {
        /// <summary>Unknown codec.</summary>
        Unknown,
        /// <summary>Linear PCM 16-bit.</summary>
        L16,
        /// <summary>Linear PCM 24-bit.</summary>
        L24,
        /// <summary>Linear PCM 32-bit.</summary>
        L32,
        /// <summary>Linear PCM 8-bit.</summary>
        L8,
        /// <summary>Opus codec.</summary>
        Opus,
        /// <summary>AC3 codec.</summary>
        AC3
    }

    /// <summary>
    /// Represents parsed SDP (Session Description Protocol) information for an AES67 audio stream.
    /// </summary>
    public record SdpInfo
    {
        /// <summary>
        /// Gets or sets the UTC time when the SDP was received.
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The duration for which the SDP info is considered active.
        /// </summary>
        public TimeSpan keepAlive = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Gets or sets the session name from the SDP.
        /// </summary>
        public string SessionName { get; set; } = "";

        /// <summary>
        /// Gets or sets the origin address from the SDP.
        /// </summary>
        public string OriginAddress { get; set; } = "";

        /// <summary>
        /// Gets or sets the multicast IP address for the audio stream.
        /// </summary>
        public IPAddress MulticastAddress { get; set; } = IPAddress.Any;

        /// <summary>
        /// Gets or sets the port number for the audio stream.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the RTP payload type.
        /// </summary>
        public int PayloadType { get; set; }

        /// <summary>
        /// Gets or sets the audio codec type.
        /// </summary>
        public AudioCodec Codec { get; set; } = AudioCodec.Unknown;

        /// <summary>
        /// Gets or sets the sample rate in Hz.
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// Gets or sets the number of audio channels.
        /// </summary>
        public int Channels { get; set; }

        /// <summary>
        /// Gets or sets the packet time in milliseconds.
        /// </summary>
        public int PTimeMs { get; set; } = 0;

        /// <summary>
        /// Gets or sets the timestamp reference clock information.
        /// </summary>
        public string TsRefClk { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether the stream is receive-only.
        /// </summary>
        public bool ReceiveOnly { get; set; } = false;

        /// <summary>
        /// Gets a value indicating whether the SDP info is still active (not expired).
        /// </summary>
        public bool Active { get { return ReceivedAt + keepAlive > DateTime.UtcNow; } }



        /// <summary>
        /// Parses an SDP string and returns an <see cref="SdpInfo"/> instance with extracted information.
        /// </summary>
        /// <param name="sdp">The SDP string to parse.</param>
        /// <returns>A populated <see cref="SdpInfo"/> instance.</returns>
        public static SdpInfo Parse(string sdp)
        {
            var info = new SdpInfo();

            foreach (var line in sdp.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var l = line.Trim();

                if (l.StartsWith("s="))
                {
                    info.SessionName = l.Substring(2).Trim();
                }
                else if (l.StartsWith("o="))
                {
                    var parts = l.Split(' ');
                    if (parts.Length >= 5)
                        info.OriginAddress = parts[4];
                }
                else if (l.StartsWith("c=IN IP4"))
                {
                    var addrPart = l.Substring("c=IN IP4".Length).Trim();
                    var slashIndex = addrPart.IndexOf('/');
                    var ipStr = slashIndex >= 0 ? addrPart.Substring(0, slashIndex) : addrPart;
                    if (IPAddress.TryParse(ipStr, out var ip))
                        info.MulticastAddress = ip;
                }
                else if (l.StartsWith("a=recvonly"))
                {
                    info.ReceiveOnly = true;
                }
                else if (l.StartsWith("m=audio"))
                {
                    var parts = l.Split(' ');
                    if (parts.Length >= 4)
                    {
                        info.Port = int.Parse(parts[1]);
                        info.PayloadType = int.Parse(parts[3]);
                    }
                }
                else if (l.StartsWith("a=rtpmap:"))
                {
                    var parts = l.Substring("a=rtpmap:".Length).Split(' ');
                    if (parts.Length >= 2)
                    {
                        if (int.TryParse(parts[0], out int pt) && pt == info.PayloadType)
                        {
                            var codecParts = parts[1].Split('/');
                            if (codecParts.Length > 0)
                                info.Codec = codecParts[0].ToUpper() switch
                                {
                                    "L16" => AudioCodec.L16,
                                    "L24" => AudioCodec.L24,
                                    "L32" => AudioCodec.L32,
                                    "L8" => AudioCodec.L8,
                                    "OPUS" => AudioCodec.Opus,
                                    "AC3" => AudioCodec.AC3,
                                    _ => AudioCodec.Unknown
                                };

                            if (codecParts.Length > 1) info.SampleRate = int.Parse(codecParts[1]);
                            if (codecParts.Length > 2) info.Channels = int.Parse(codecParts[2]);
                        }
                    }
                }
                else if (l.StartsWith("a=ptime:"))
                {
                    if (int.TryParse(l.Substring("a=ptime:".Length), out int ptime))
                        info.PTimeMs = ptime;
                }
                else if (l.StartsWith("a=ts-refclk:"))
                {
                    info.TsRefClk = l.Substring("a=ts-refclk:".Length).Trim();
                }
            }

            return info;
        }

        /// <summary>
        /// Returns a string representation of the SDP information.
        /// </summary>
        /// <returns>A formatted string with SDP details.</returns>
        public override string ToString()
        {
            return $"Session: {SessionName}\nOrigin: {OriginAddress}\nMulticast: {MulticastAddress}\nPort: {Port}\nPayloadType: {PayloadType}\nCodec: {Codec}\nSampleRate: {SampleRate}\nChannels: {Channels}\nPTime(ms): {PTimeMs}\nReceiveOnly: {ReceiveOnly}\nPTP Clock: {TsRefClk}";
        }
    }
}
