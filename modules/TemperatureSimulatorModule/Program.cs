namespace TemperatureSimulatorModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;

    class Program
    {
        static readonly Random Rnd = new Random();
        
        public static int Main(string[] args) => MainAsync(args).Result;

        static async Task<int> MainAsync(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            ModuleClient moduleClient = await Init(configuration);

            TimeSpan messageDelay = configuration.GetValue("MessageDelay", TimeSpan.FromSeconds(5));
            var sim = new SimulatorParameters
            {
                TempMin = configuration.GetValue<double>("tempMin", 21),
                TempMax = configuration.GetValue<double>("tempMax", 100),
            };

            await SendEvents(moduleClient, messageDelay, sim).ConfigureAwait(false);

            return 0;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task<ModuleClient> Init(IConfiguration configuration)
        {
            AmqpTransportSettings amqpSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { amqpSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            return ioTHubModuleClient;
        }

        static async Task SendEvents(ModuleClient moduleClient, TimeSpan messageDelay, SimulatorParameters sim)
        {
            int count = 1;
            double currentTemp = sim.TempMin;
            var cts = new CancellationTokenSource();

            while (!cts.Token.IsCancellationRequested)
            {
                if (currentTemp > sim.TempMax)
                {
                    currentTemp += Rnd.NextDouble() - 0.5; // add value between [-0.5..0.5]
                }
                else
                {
                    currentTemp += -0.25 + (Rnd.NextDouble() * 1.5); // add value between [-0.25..1.25] - average +0.5
                }

                var tempData = new MessageBody
                {
                    Temperature = currentTemp,
                    Humidity = Rnd.Next(24, 27),
                    TimeCreated = DateTime.UtcNow
                };

                string dataBuffer = JsonConvert.SerializeObject(tempData);
                var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                Console.WriteLine($"\t{DateTime.Now.ToLocalTime()}> Sending message: {count}, Body: [{dataBuffer}]");

                await moduleClient.SendEventAsync("temperatureOutput", eventMessage).ConfigureAwait(false);
                await Task.Delay(messageDelay, cts.Token).ConfigureAwait(false);
                count++;
            }
        }

        
        internal class SimulatorParameters
        {
            public double TempMin { get; set; }

            public double TempMax { get; set; }
        }
    }
}
