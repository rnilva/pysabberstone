using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using SabberStone_gRPC.MMF;
using SabberStone_gRPC.MMF.Entities;
using SabberStonePython.API;
using SabberStonePython.Tests;

namespace SabberStone_gRPC
{
    class Program
    {
        const int DEFAULT_PORT = 50052;

        static void Main(string[] args)
        {
            //if (args.Length > 1 && !int.TryParse(args[0], out int port))
            //    throw new ArgumentException($"Cannot parse port number from given argument {args[0]}.");

            //port = DEFAULT_PORT;

            //var server = new ServerHandleImpl(port);

            //server.Start();

            //server.Shutdown().Wait();

            //Debugger.DebugRun().Wait();

            //FileLockTest.Test();
            //MMFTest.Test();
            //MMFTest.MarshalTest();

            //PythonHelper.WritePythonEntities();
            //MMFServer.Run();
            //HandZone_unmanaged.Test();
            PerformanceComparison.MarshalEntity();
        }
    }

    public class ServerHandleImpl : SabberStonePython.API.ServerHandle.ServerHandleBase
    {
        public Server Server { get; private set; }

        public ServerHandleImpl(int port)
        {
            Server = new Server
            {
                Services =
                {
                    SabberStonePython.API.SabberStonePython.BindService(new API()),
                    SabberStonePython.API.ServerHandle.BindService(this)
                },
                Ports = {new ServerPort("0.0.0.0", port, ServerCredentials.Insecure)}
            };
        }

        public override Task<SabberStonePython.API.Empty> Close(SabberStonePython.API.Empty request, ServerCallContext context)
        {
            Console.WriteLine("Closing......");

            Server.ShutdownAsync();
            return Task.FromResult(new SabberStonePython.API.Empty());
        }

        public void Start()
        {
            Server.Start();

            Console.WriteLine("SabberStone gRPC Server listening on port " + Server.Ports.First().Port);
            Console.WriteLine("Call ServerHandle.Close() to terminate.");
        }

        public async Task Shutdown()
        {
            await Server.ShutdownTask;
            
            Console.WriteLine("Server terminated!");
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

