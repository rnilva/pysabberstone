using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using SabberStone_gRPC;

namespace SabberStone_gRPC_Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //ConnectAsync("localhost:50052", "Client1");
            //ConnectAsync("localhost:50052", "Client2");
            //ConnectAsync("localhost:50052", "Client3");

            //SabberServiceTest();


            Console.ReadKey();
        }

        static async void SabberServiceTest()
        {
            var channel = new Channel("localhost:50052", ChannelCredentials.Insecure);
            await channel.ConnectAsync();

            var game = new SabberStoneRPC.SabberStoneRPCClient(channel);
            var empty = new Empty();

            game.CreateGame(empty);

        }

        static async Task ConnectAsync(string target, string clientName)
        {
            var channel = new Channel(target, ChannelCredentials.Insecure);
            await channel.ConnectAsync();

            Console.WriteLine(clientName + " Connected.");

            var client = new Greeter.GreeterClient(channel);
            using (var server = client.Bidirectional(
                headers: new Metadata {new Metadata.Entry("name", clientName)}))
            {
                var responseReaderTask = Task.Run(async () =>
                {
                    while (await server.ResponseStream.MoveNext(CancellationToken.None))
                    {
                        SabberResponse response = server.ResponseStream.Current;

                        Console.WriteLine("Response received: " + response.Message);;
                    }
                });

                await server.RequestStream.WriteAsync(new SabberRequest
                {
                    Type = SabberRequest.Types.Type.Test1
                });

                await server.RequestStream.CompleteAsync();
                await responseReaderTask;

                Console.WriteLine("Client " + clientName + " is disconnected.");
            }
        }
    }
}
