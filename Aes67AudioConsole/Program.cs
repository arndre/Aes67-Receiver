using Aes67Receiver;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Net;

namespace Aes67AudioConsole
{
    /// <summary>
    /// Entry point for the AES67 Audio Console application.
    /// Handles device discovery, wave provider creation, audio mixing, and playback.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Logger instance for diagnostic output.
        /// </summary>
        private static readonly ILogger logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Program>();

        /// <summary>
        /// Service for discovering AES67 SDP streams via SAP multicast.
        /// </summary>
        private static SdpDiscoveryService? discovery;

        /// <summary>
        /// Cancellation token source for application lifetime.
        /// </summary>
        static CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>
        /// Stores wave providers for each discovered SDP stream.
        /// </summary>
        private static ConcurrentDictionary<SdpInfo, Aes67BufferedWaveProvider> waveProviders = new ConcurrentDictionary<SdpInfo, Aes67BufferedWaveProvider>();

        /// <summary>
        /// Output device for audio playback.
        /// </summary>
        private static WaveOutEvent? waveOut;


        /// <summary>
        /// Main entry point for the application.
        /// Handles initialization, device discovery, wave provider creation, mixing, and playback.
        /// </summary>
        /// <param name="args">Command-line arguments. Optionally, the multicast IP address for SAP discovery.</param>
        static void Main(string[] args)
        {
            Console.WriteLine("AES67 Audio Receiver Console");

            // Parse multicast address from arguments or use default
            if (args.Length <= 0 || IPAddress.TryParse(args[0], out IPAddress ipAddressMulticast))
            {
                ipAddressMulticast = IPAddress.Parse("239.255.255.255");
            }

            // +++++ Device discovery +++++
            CancellationTokenSource ctsDiscovery = new CancellationTokenSource();
            discovery = new SdpDiscoveryService(ctsDiscovery.Token);

            // Start discovery in background
            Task.Run(async () => await discovery.Run(ipAddressMulticast));
            Console.WriteLine("Press a key to stop discovery");

            // Wait for user input to stop discovery
            Console.ReadLine();
            ctsDiscovery.Cancel();
            // +++++ End device discovery +++++

            // Create wave providers for each discovered SDP stream
            CreateWaveProvider(discovery.DiscoveredSdpInfos.ToList());

            var mixingWaveProvider = MixWaveProviders();

            InitWaveOut(mixingWaveProvider);

            Console.ReadLine();
        }

        private static MixingWaveProvider32 MixWaveProviders()
        {
            var mixingWaveProvider = new MixingWaveProvider32();

            foreach (var provider in waveProviders.Values)
            {
                // Convert 24 bit to 32 bit for mixing
                var wp32 = new Wave24To32Provider(provider);

                if (wp32.WaveFormat.Channels > 1)
                {
                    // Split multichannel stream into separate mono streams
                    var w = new Helper.MultiChannelSplitter(wp32);

                    // Add each channel separately to the mixer
                    foreach (var waveProvider in w.ChannelBuffers)
                    {
                        mixingWaveProvider.AddInputStream(waveProvider);
                    }
                }
                else
                {
                    // Add mono stream directly
                    mixingWaveProvider.AddInputStream(wp32);
                }
            }

            return mixingWaveProvider;
        }

        /// <summary>
        /// Creates wave providers for each discovered SDP stream.
        /// Each provider is configured for buffering and playback.
        /// </summary>
        private static void CreateWaveProvider(List<SdpInfo> sdpInfos)
        {
            logger.LogInformation("Create wave provider");
            foreach (var sdpInfo in sdpInfos)
            {
                var waveFormat = Helper.WaveFormatFromSdpInfo(sdpInfo);

                waveProviders[sdpInfo] = new Aes67BufferedWaveProvider(waveFormat, sdpInfo, cts.Token) { BufferDuration = TimeSpan.FromMilliseconds(100) };

                logger.LogInformation($"Created AES67 wave provider for SDP Session: {sdpInfo.SessionName}, {waveFormat}");
            }
        }

        /// <summary>
        /// Initializes the audio output device and starts playback.
        /// </summary>
        private static void InitWaveOut(IWaveProvider waveProvider)
        {
            waveOut = new WaveOutEvent() { DesiredLatency = 100 };
            waveOut.DeviceNumber = 0;
            waveOut.Init(waveProvider);
            waveOut.Play();
        }
    }
}
