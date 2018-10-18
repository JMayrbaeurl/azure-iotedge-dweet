namespace DweetModule
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;

    class Program
    {
        static int counter;

        private static readonly HttpClient client = new HttpClient();

        static string dweetThingname;

        static bool verbose = true;

        static void Main(string[] args)
        {
            Init(args).Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init(string[] args)
        {
            AmqpTransportSettings amqpSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { amqpSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();

            Console.WriteLine("Dweet module client initialized. Waiting on input1 for messages.");

            // Register callback to be called when a message is received by the module
            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", ForwardMessageToDweet, ioTHubModuleClient);

            if (args != null && args.Length >= 1)
                dweetThingname = args[0];
            else {
                dweetThingname = System.Environment.GetEnvironmentVariable("DWEETTHINGNAME");
                if (dweetThingname == null || dweetThingname.Length == 0)
                    dweetThingname = GetDweetThingsname(false);
            }
            Console.WriteLine($"Using '{dweetThingname}' as dweet thing name");

            if (args != null && args.Length >= 2) {
                if (args[1].Equals("-verbose"))
                    verbose = true;
            }
        }

        static string GetDweetThingsname(bool addModulename) {

            string newDweetThingname = "IoTEdgeInstance";

            string fullIoTHubname = System.Environment.GetEnvironmentVariable("IOTEDGE_IOTHUBHOSTNAME");
            if (fullIoTHubname != null && fullIoTHubname.Length > 0) {
                if (addModulename)
                    newDweetThingname = fullIoTHubname.Split('.')[0] +  "_" + System.Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
                else
                    newDweetThingname = fullIoTHubname.Split('.')[0];
            }

            return newDweetThingname;
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> ForwardMessageToDweet(Message message, object userContext)
        {
            int counterValue = Interlocked.Increment(ref counter);

            var moduleClient = userContext as ModuleClient;
            if (moduleClient == null)
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            byte[] messageBytes = message.GetBytes();
            string messageString = Encoding.UTF8.GetString(messageBytes);
            if (verbose)
                Console.WriteLine($"Received message: {counterValue}, Body: [{messageString}]");

            if (!string.IsNullOrEmpty(messageString))
            {
                var content = new StringContent(messageString);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                string dweetURL = "https://dweet.io/dweet/for/" + dweetThingname;

                if (verbose)
                    Console.WriteLine("Forward message to dweet: '" + dweetURL + "'");

                HttpResponseMessage response =  await client.PostAsync(dweetURL, content);
                if (response.IsSuccessStatusCode) {
                    if (verbose)
                        Console.WriteLine("Received message fowarded to dweet");
                } else
                    Console.WriteLine("Error response from Dweet: " + response.ReasonPhrase);
            } else {
                if (verbose)
                    Console.WriteLine("Empty message string received!");
            }

            return MessageResponse.Completed;
        }
    }
}
