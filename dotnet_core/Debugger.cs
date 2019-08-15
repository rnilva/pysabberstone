using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using SabberStonePython.API;

namespace SabberStone_gRPC
{
    public static class Debugger
    {
        public static async Task DebugRun()
        {
            var server = new ServerHandleImpl(50052);

            server.Start();

            var channel = new Channel("localhost:50052", ChannelCredentials.Insecure);
            await channel.ConnectAsync();

            var stub = new SabberStonePython.API.SabberStonePython.SabberStonePythonClient(channel);

            int count = 100;
            var watch = Stopwatch.StartNew();
            FullRandomGame(stub, @"AAEBAf0EAA8MLU1xwwG7ApUDrgO/A4AEtATmBO0EoAW5BgA=", count);
            watch.Stop();
            Console.WriteLine($"{count} random games: {watch.ElapsedMilliseconds / 1000.0} sec");

            //server.Shutdown().Wait();
        }

        public static void FullRandomGame(
            SabberStonePython.API.SabberStonePython.SabberStonePythonClient stub,
            string deck, int count)
        {
            var rnd = new Random();

            Game game = stub.NewGame(new DeckStrings
            {
                Deck1 = deck,
                Deck2 = deck
            });

            var id = new GameId(game.Id);

            for (int i = 0; i < count; i++)
            {
                while (game.State != Game.Types.State.Complete)
                {
                    Options options = stub.GetOptions(id);
                    Option option = options.List[rnd.Next(options.List.Count)];
                    game = stub.Process(option);
                }

                game = stub.Reset(id);

                Console.WriteLine($"{i + 1}th game is finished.");
            }


        }
    }
}
