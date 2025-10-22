using Aes67Receiver;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Reflection;

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
            var version = Assembly.GetExecutingAssembly()
                             .GetName()
                             .Version?
                             .ToString() ?? "unknown";

            Console.WriteLine($"AES67 Receiver Console v{version}");
            Console.WriteLine();

            // Parse multicast address from arguments or use default
            if (args.Length <= 0 || IPAddress.TryParse(args[0], out IPAddress ipAddressMulticast))
            {
                ipAddressMulticast = IPAddress.Parse("239.255.255.255");
            }


            if (ipAddressMulticast == null)
            {
                logger.LogError("Invalid multicast IP address provided.");
                return;
            }


            // Init device discovery
            var ctsDiscovery = new CancellationTokenSource();

            // Create SDP discovery service
            var discovery = new SdpDiscoveryService();

            Console.WriteLine($"Starting device discovery on multicast address {ipAddressMulticast}...");

            // Start discovery in background and wait for key input
            Task.Run( async () => await discovery.Run(ipAddressMulticast, ctsDiscovery.Token));
            Console.WriteLine("Press Enter to stop discovery");
            Console.WriteLine();
            Console.ReadLine();
            ctsDiscovery.Cancel();
            Console.WriteLine("Device discovery has stopped");

            
            var sdpInfos = discovery.DiscoveredSdpInfos.ToList();

            if (!sdpInfos.Any())
            {
                logger.LogError("No SDP streams discovered. Exiting application.");
                return;
            }

            // Create wave providers for each discovered SDP stream
            CreateWaveProviders(sdpInfos);

            // Mix all incoming audio
            var mixingWaveProvider = MixWaveProviders();

            // Start playback 
            InitWaveOut(mixingWaveProvider);

            Console.WriteLine();
            Console.WriteLine("Press Enter to stop playback and exit...");

            Console.ReadLine();

            if(waveOut != null)
            {
                Console.WriteLine("Clean up wave output");
                waveOut.Stop();
                waveOut.Dispose();
            }

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
                    logger.LogInformation("Splitting {name} {channels}-channel stream into separate mono streams for mixing", provider.SdpInfo.SessionName, wp32.WaveFormat.Channels);
                    var w = new Helper.MultiChannelSplitter(wp32);

                    // Add each channel separately to the mixer
                    foreach (var waveProvider in w.ChannelBuffers)
                    {
                        logger.LogInformation($"Adding channel {waveProvider.WaveFormat.Channels} to mixer");
                        mixingWaveProvider.AddInputStream(waveProvider);
                    }
                }
                else
                {
                    logger.LogInformation("Adding mono stream {name} to mixer", provider.SdpInfo.SessionName);

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
        private static void CreateWaveProviders(List<SdpInfo> sdpInfos)
        {
            logger.LogInformation("Create wave providers");
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

            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();

            // Get the default audio output device
            var device = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).FirstOrDefault();

            if (device == null)
            {
                logger.LogError("No audio output device found.");
                return;
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine($"Using output device: {device.FriendlyName}");
                waveOut = new WaveOutEvent() { DesiredLatency = 100 }; //
                waveOut.DeviceNumber = 0; // Use default device

                waveOut.Init(waveProvider);
                waveOut.Play();
            }
        }
    }
}
