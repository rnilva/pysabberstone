using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using SabberStonePython.API;
using SabberStonePython.Tests;

namespace SabberStone_gRPC
{
    class Program
    {
        static void Main(string[] args)
        {
            //UtilTests.DeckStringDeserialize();


            Console.WriteLine("Test");

            const int PORT = 50052;

            //Grpc.Core.Interceptors.

            var server = new Server
            {
                Services =
                {
                    //Greeter.BindService(new GreeterImpl()),
                    //SabberStoneRPC.BindService(new SabberStoneRPCImpl())
                    SabberStonePython.API.SabberStonePython.BindService(new API())
                },
                Ports = {new ServerPort("localhost", PORT, ServerCredentials.Insecure)}
            };
            server.Start();

            Console.WriteLine("SabberStone gRPC Server listening on port " + PORT);
            //Console.WriteLine("Press any key to stop the server...");
            //Console.ReadKey();

            while (true)
            {
                Console.WriteLine("Write a name of the client to communicate!");
                string command = Console.ReadLine();
                if (command == "stop") break;
                if (command == "disconnect")
                {
                    Console.Write("Write a name of the client to disconnect: ");
                    ClientManager.Disconnect(Console.ReadLine());
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Write a message to send.");
                    string message = Console.ReadLine();

                    ClientManager.SendMessage(command, message);
                }
            }


            server.ShutdownAsync().Wait();
        }
    }

    public static class ClientManager
    {
        public static ConcurrentDictionary<string, Client> ClientDictionary
            = new ConcurrentDictionary<string, Client>();

        public static void SendMessage(string clientName, string message)
        {
            ClientDictionary[clientName].SendMessage(message);
        }

        public static void Disconnect(string clientName)
        {
            ClientDictionary[clientName].Disconnect();
            //ClientDictionary.TryRemove(clientName, out _);
        }
    }

    public class Client
    {
        private readonly IAsyncStreamReader<SabberRequest> _requestStream;
        private readonly IServerStreamWriter<SabberResponse> _responseStream;
        private readonly ServerCallContext _context;
        private readonly CancellationTokenSource _cts;

        private TaskCompletionSource<SabberResponse> _tcs;

        public string Name { get; }

        public CancellationToken CancellationToken => _cts.Token;

        public Client(string name, 
            IAsyncStreamReader<SabberRequest> requestStream, 
            IServerStreamWriter<SabberResponse> responseStream, 
            ServerCallContext context)
        {
            _requestStream = requestStream;
            _responseStream = responseStream;
            _context = context;
            _cts = new CancellationTokenSource();

            Name = name;

            ClientManager.ClientDictionary.TryAdd(name, this);

            Console.WriteLine("Client " + name + " is created.");
        }

        public async Task WaitMessage()
        {
            CancellationToken token = _cts.Token;
            while (true)
            {
                var tcs = new TaskCompletionSource<SabberResponse>();

                _tcs = tcs;

                Console.WriteLine("Client " + Name + " is waiting a message.");

                try
                {
                    await tcs.Task;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Client {Name} is disconnected.");
                    break;
                }

                if (tcs.Task.IsCanceled)
                {
                    Console.WriteLine($"Client {Name} is disconnected.");
                    break;
                }

                Console.WriteLine("Waiting ends.");

                SabberResponse response = tcs.Task.Result;

                Console.WriteLine($"Client {Name} gets a response {response.Message}.");

                _responseStream.WriteAsync(response);

                Console.WriteLine("Message \"" + response.Message + "\" is sent to " + Name);
            }
        }

        public void Disconnect()
        {
            _tcs.SetCanceled();
            _cts.Cancel();
        }

        public void SendMessage(string message)
        {
            if (_tcs == null)
                throw new Exception();

            _tcs.SetResult(new SabberResponse
            {
                Message = message
            });
        }
    }

    public class GreeterImpl : Greeter.GreeterBase
    {
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello from SabberStone gRPC Server!"
            });
        }

        public override Task<Empty> HelloEmpty(Empty request, ServerCallContext context)
        {
            return Task<Empty>.Factory.StartNew(() =>
            {
                Console.WriteLine("HelloEmpty called.");
                return new Empty();
            });
        }

        public override async Task Bidirectional(IAsyncStreamReader<SabberRequest> requestStream, IServerStreamWriter<SabberResponse> responseStream, ServerCallContext context)
        {
            string clientName = context.RequestHeaders.Single(e => e.Key == "name").Value;

            Client client = new Client(clientName, requestStream, responseStream, context);

            var requestReaderTask = Task.Run(async () =>
            {
                while (await requestStream.MoveNext(client.CancellationToken))
                    Console.WriteLine(
                        $"Server receives a message of type {requestStream.Current.Type} from Client {clientName}");
            });

            await client.WaitMessage();

            Console.WriteLine("Message waiting is ended.");

            await requestReaderTask;

            Console.WriteLine("Connection between " + clientName + " is closed.");
        }
    } 
}

