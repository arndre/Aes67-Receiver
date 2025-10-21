using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aes67AudioConsole
{
    using NAudio.Wave;
    using System;

    /// <summary>
    /// Provides a buffered wave provider that converts 24-bit PCM audio samples to 32-bit IEEE float samples.
    /// </summary>
    internal class BufferedWaveProvider24To32 : BufferedWaveProvider
    {
        /// <summary>
        /// The underlying buffered wave provider containing 24-bit PCM samples.
        /// </summary>
        private readonly BufferedWaveProvider inputBufferedWaveProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferedWaveProvider24To32"/> class.
        /// </summary>
        /// <param name="inputBufferedWaveProvider">The input buffered wave provider with 24-bit PCM samples.</param>
        /// <exception cref="ArgumentException">Thrown if the input wave format is not 24-bit PCM.</exception>
        public BufferedWaveProvider24To32(BufferedWaveProvider inputBufferedWaveProvider)
            : base(WaveFormat.CreateIeeeFloatWaveFormat(inputBufferedWaveProvider.WaveFormat.SampleRate,
                                                        inputBufferedWaveProvider.WaveFormat.Channels))
        {
            if (inputBufferedWaveProvider.WaveFormat.BitsPerSample != 24)
                throw new ArgumentException("Input WaveFormat must be 24-bit PCM.");

            this.inputBufferedWaveProvider = inputBufferedWaveProvider;
        }

        /// <summary>
        /// Reads audio data from the input buffer, converts 24-bit PCM samples to 32-bit float samples, and writes them to the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer to write the converted samples to.</param>
        /// <param name="offset">The offset in the buffer at which to begin writing.</param>
        /// <param name="count">The maximum number of bytes to write to the buffer.</param>
        /// <returns>The number of bytes written to the buffer.</returns>
        public int Read(byte[] buffer, int offset, int count)
        {
            int bytesPerSampleOut = 4; // 32-bit float = 4 bytes
            int samplesRequested = count / bytesPerSampleOut;
            int bytesNeeded = samplesRequested * 3; // 3 bytes per 24-bit sample

            byte[] tempBuffer = new byte[bytesNeeded];
            int bytesRead = inputBufferedWaveProvider.Read(tempBuffer, 0, bytesNeeded);
            int samplesRead = bytesRead / 3;

            for (int i = 0; i < samplesRead; i++)
            {
                int index24 = i * 3;
                int indexFloat = offset + i * 4;

                // Convert 24-bit PCM to signed int
                int sample24 = tempBuffer[index24] | (tempBuffer[index24 + 1] << 8) | (tempBuffer[index24 + 2] << 16);
                if ((sample24 & 0x800000) != 0)
                    sample24 |= unchecked((int)0xFF000000); // sign extend

                // Normalize to float (-1.0f … 1.0f)
                float sampleFloat = sample24 / 8388608f;

                // Convert float to bytes (little-endian)
                byte[] floatBytes = BitConverter.GetBytes(sampleFloat);
                Buffer.BlockCopy(floatBytes, 0, buffer, indexFloat, 4);
            }

            return samplesRead * 4; // number of bytes written
        }
    }

}
