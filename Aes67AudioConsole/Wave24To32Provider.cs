using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aes67AudioConsole
{
    internal class Wave24To32Provider : IWaveProvider
    {
        private readonly BufferedWaveProvider source;
        public WaveFormat WaveFormat { get; }

        public Wave24To32Provider(BufferedWaveProvider source)
        {
            if (source.WaveFormat.BitsPerSample != 24)
                throw new ArgumentException("Source must be 24-bit PCM");
            this.source = source;
            this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, source.WaveFormat.Channels);
        }

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
