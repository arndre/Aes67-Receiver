using Aes67Receiver;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aes67AudioConsole
{
    /// <summary>
    /// Provides helper methods and classes for AES67 audio processing.
    /// </summary>
    internal static class Helper
    {
        /// <summary>
        /// Creates a <see cref="WaveFormat"/> instance from the specified <see cref="SdpInfo"/>.
        /// </summary>
        /// <param name="info">The SDP information describing the audio stream.</param>
        /// <returns>A <see cref="WaveFormat"/> matching the SDP parameters.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="info"/> is null.</exception>
        /// <exception cref="NotSupportedException">Thrown if the codec specified in <paramref name="info"/> is not supported.</exception>
        public static WaveFormat WaveFormatFromSdpInfo(SdpInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            int sampleRate = info.SampleRate > 0 ? info.SampleRate : 48000;
            int channels = info.Channels > 0 ? info.Channels : 2;

            // AES67 typically uses 24-bit or 32-bit PCM (big endian)
            switch (info.Codec)
            {
                case AudioCodec.L16:
                    return WaveFormat.CreateCustomFormat(
                        WaveFormatEncoding.Pcm,
                        sampleRate,
                        channels,
                        sampleRate * channels * 2, // bytes per sec
                        channels * 2,              // block align
                        16);

                case AudioCodec.L24:
                    return WaveFormat.CreateCustomFormat(
                        WaveFormatEncoding.Pcm,
                        sampleRate,
                        channels,
                        sampleRate * channels * 3,
                        channels * 3,
                        24);

                case AudioCodec.L32:
                    return WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);

                default:
                    throw new NotSupportedException($"Unsupported codec: {info.Codec}");
            }
        }

        /// <summary>
        /// Splits multi-channel audio data from an <see cref="IWaveProvider"/> into separate per-channel buffers.
        /// </summary>
        public class MultiChannelSplitter
        {
            /// <summary>
            /// Gets the array of <see cref="BufferedWaveProvider"/> instances, one for each channel.
            /// </summary>
            public BufferedWaveProvider[] ChannelBuffers { get; }

            private readonly int channels;
            private readonly int bytesPerSample;

            /// <summary>
            /// Initializes a new instance of the <see cref="MultiChannelSplitter"/> class for the specified source.
            /// </summary>
            /// <param name="source">The source <see cref="IWaveProvider"/> containing multi-channel audio data.</param>
            public MultiChannelSplitter(IWaveProvider source)
            {
                channels = source.WaveFormat.Channels;
                bytesPerSample = source.WaveFormat.BitsPerSample / 8;

                ChannelBuffers = new BufferedWaveProvider[channels];

                for (int c = 0; c < channels; c++)
                {
                    ChannelBuffers[c] = new BufferedWaveProvider(WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1));
                }
            }

            /// <summary>
            /// Reads samples from the source and splits them into per-channel buffers.
            /// </summary>
            /// <param name="source">The source <see cref="IWaveProvider"/> to read from.</param>
            /// <param name="samplesToRead">The number of samples to read and split.</param>
            public void ReadAndSplit(IWaveProvider source, int samplesToRead)
            {
                int blockSize = samplesToRead * channels * bytesPerSample;
                byte[] buffer = new byte[blockSize];
                int bytesRead = source.Read(buffer, 0, blockSize);

                int sampleCount = bytesRead / (bytesPerSample * channels);

                for (int s = 0; s < sampleCount; s++)
                {
                    for (int c = 0; c < channels; c++)
                    {
                        int sourceIndex = (s * channels + c) * bytesPerSample;
                        ChannelBuffers[c].AddSamples(buffer, sourceIndex, bytesPerSample);
                    }
                }
            }
        }
    }
}
