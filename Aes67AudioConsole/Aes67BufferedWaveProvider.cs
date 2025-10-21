using Aes67Receiver;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Aes67AudioConsole
{
    /// <summary>
    /// Provides a buffered wave provider for AES67 audio streams, receiving samples via multicast RTP.
    /// </summary>
    internal class Aes67BufferedWaveProvider : BufferedWaveProvider
    {
        /// <summary>
        /// Gets the SDP (Session Description Protocol) information for the AES67 stream.
        /// </summary>
        public SdpInfo SdpInfo { private set; get; }

        /// <summary>
        /// The receiver instance responsible for receiving AES67 RTP packets.
        /// </summary>
        private Receiver receiver;

        /// <summary>
        /// Initializes a new instance of the <see cref="Aes67BufferedWaveProvider"/> class.
        /// </summary>
        /// <param name="waveFormat">The audio wave format for the buffer.</param>
        /// <param name="sdpInfo">The SDP information describing the AES67 stream.</param>
        /// <param name="ct">Cancellation token to signal receiver shutdown.</param>
        public Aes67BufferedWaveProvider(WaveFormat waveFormat, SdpInfo sdpInfo, CancellationToken ct) : base(waveFormat)
        {
            SdpInfo = sdpInfo;

            DiscardOnBufferOverflow = true;

            receiver = new Receiver(ct, sdpInfo.MulticastAddress);
            receiver.SamplesReceived += OnSamplesReceived;
            receiver.Start();
        }

        /// <summary>
        /// Handles received audio samples and adds them to the buffer.
        /// </summary>
        /// <param name="buffer">The audio sample bytes received from the RTP stream.</param>
        private void OnSamplesReceived(byte[] buffer)
        {
            this.AddSamples(buffer, 0, buffer.Length);
        }
    }

}
