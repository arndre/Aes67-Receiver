using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aes67Receiver
{
    /// <summary>
    /// Represents the header of an RTP (Real-time Transport Protocol) packet.
    /// </summary>
    /// <remarks>
    /// This record provides properties for all standard RTP header fields and a static method to parse them from a byte array.
    /// </remarks>
    public record RtpHeader
    {
        /// <summary>
        /// Gets the RTP version.
        /// </summary>
        public byte Version { get; init; }

        /// <summary>
        /// Gets a value indicating whether the RTP packet contains padding bytes.
        /// </summary>
        public bool Padding { get; init; }

        /// <summary>
        /// Gets a value indicating whether the RTP packet contains an extension header.
        /// </summary>
        public bool Extension { get; init; }

        /// <summary>
        /// Gets the number of CSRC identifiers included in the header.
        /// </summary>
        public byte CsrcCount { get; init; }

        /// <summary>
        /// Gets a value indicating whether the marker bit is set.
        /// </summary>
        public bool Marker { get; init; }

        /// <summary>
        /// Gets the payload type identifier.
        /// </summary>
        public byte PayloadType { get; init; }

        /// <summary>
        /// Gets the sequence number of the RTP packet.
        /// </summary>
        public ushort SequenceNumber { get; init; }

        /// <summary>
        /// Gets the timestamp of the RTP packet.
        /// </summary>
        public uint Timestamp { get; init; }

        /// <summary>
        /// Gets the synchronization source (SSRC) identifier.
        /// </summary>
        public uint Ssrc { get; init; }

        /// <summary>
        /// Parses the RTP header from the specified byte array.
        /// </summary>
        /// <param name="data">The byte array containing the RTP packet data.</param>
        /// <returns>An <see cref="RtpHeader"/> instance with parsed header fields.</returns>
        /// <exception cref="ArgumentException">Thrown when the data array is too short to contain a valid RTP header.</exception>
        public static RtpHeader Parse(byte[] data)
        {
            if (data.Length < 12)
                throw new ArgumentException("Invalid RTP packet");

            var h = new RtpHeader
            {
                Version = (byte)(data[0] >> 6),
                Padding = (data[0] & 0x20) != 0,
                Extension = (data[0] & 0x10) != 0,
                CsrcCount = (byte)(data[0] & 0x0F),
                Marker = (data[1] & 0x80) != 0,
                PayloadType = (byte)(data[1] & 0x7F),
                SequenceNumber = (ushort)((data[2] << 8) | data[3]),
                Timestamp = (uint)((data[4] << 24) | (data[5] << 16) | (data[6] << 8) | data[7]),
                Ssrc = (uint)((data[8] << 24) | (data[9] << 16) | (data[10] << 8) | data[11])
            };

            return h;
        }

        /// <summary>
        /// Returns a string representation of the RTP header.
        /// </summary>
        /// <returns>A string containing the version, payload type, sequence number, timestamp, and SSRC.</returns>
        public override string ToString() =>
            $"RTP v{Version}, PT={PayloadType}, Seq={SequenceNumber}, TS={Timestamp}, SSRC={Ssrc:X8}";
    }

}
