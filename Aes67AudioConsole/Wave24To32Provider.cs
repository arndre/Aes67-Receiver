    using NAudio.Wave;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    namespace Aes67AudioConsole
    {
        /// <summary>
        /// Converts 24-bit PCM audio samples from a <see cref="BufferedWaveProvider"/> to 32-bit IEEE float samples.
        /// </summary>
        internal class Wave24To32Provider : IWaveProvider
        {
            private readonly BufferedWaveProvider source;

            /// <summary>
            /// Gets the output wave format (32-bit IEEE float).
            /// </summary>
            public WaveFormat WaveFormat { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Wave24To32Provider"/> class.
            /// </summary>
            /// <param name="source">The source <see cref="BufferedWaveProvider"/> containing 24-bit PCM audio.</param>
            /// <exception cref="ArgumentException">Thrown if the source is not 24-bit PCM.</exception>
            public Wave24To32Provider(BufferedWaveProvider source)
            {
                if (source.WaveFormat.BitsPerSample != 24)
                    throw new ArgumentException("Source must be 24-bit PCM");
                this.source = source;
                this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, source.WaveFormat.Channels);
            }

            /// <summary>
            /// Reads audio data from the source, converts 24-bit PCM samples to 32-bit IEEE float samples, and writes them to the buffer.
            /// </summary>
            /// <param name="buffer">The buffer to write the converted samples to.</param>
            /// <param name="offset">The offset in the buffer to start writing.</param>
            /// <param name="count">The maximum number of bytes to write to the buffer.</param>
            /// <returns>The number of bytes written to the buffer.</returns>
            public int Read(byte[] buffer, int offset, int count)
            {
                int samplesRequested = count / 4;
                int bytesNeeded = samplesRequested * 3;

                byte[] temp = new byte[bytesNeeded];
                int bytesRead = source.Read(temp, 0, bytesNeeded);
                int samplesRead = bytesRead / 3;

                for (int i = 0; i < samplesRead; i++)
                {
                    int index24 = i * 3;
                    int index32 = offset + i * 4;

                    int sample24 = temp[index24] | (temp[index24 + 1] << 8) | (temp[index24 + 2] << 16);
                    if ((sample24 & 0x800000) != 0)
                        sample24 |= unchecked((int)0xFF000000);

                    float sampleFloat = sample24 / 8388608f;
                    Buffer.BlockCopy(BitConverter.GetBytes(sampleFloat), 0, buffer, index32, 4);
                }

                return samplesRead * 4;
            }
        }

    }
