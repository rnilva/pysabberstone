using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;


namespace SabberStonePython.API
{
    public class API : SabberStonePython.SabberStonePythonBase
    {
        public override Task<Game> NewGame(DeckStrings request, ServerCallContext context)
        {
            Console.WriteLine("NewGame service is called!");
            Console.WriteLine("Deckstring #1: " + request.Deck1);
            Console.WriteLine("Deckstring #2: " + request.Deck2);

            return Task.FromResult(SabberHelpers.GenerateGameAPI(request.Deck1, request.Deck2));
        }

        public override Task<Options> GetOptions(Game request, ServerCallContext context)
        {
            var game = SabberHelpers.ManagedObjects.Games[request.Id];
            var options = game.CurrentPlayer.Options();

            return Task.FromResult(new Options(options, request.Id));
        }

        public override Task<Game> Process(Option request, ServerCallContext context)
        {
            Game test()
            {
                var game = SabberHelpers.ManagedObjects.Games[request.GameId];
                try
                {
                    // Use option ids instead?
                    var playerTask = SabberHelpers.GetPlayerTask(request, game);

                    Console.WriteLine(SabberHelpers.Printers.PrintAction(playerTask));

                    game.Process(playerTask);

                    Console.WriteLine(SabberHelpers.Printers.PrintGame(game));

                    return new Game(game, request.GameId);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    return null;
                }
            }

            return Task.FromResult(test());
        }

        public override Task<Cards> GetCardDictionary(Empty request, ServerCallContext context)
        {
            return Task.Factory.StartNew(() =>
            {
                var cards = new Cards(SabberStoneCore.Model.Cards.All);

                Console.WriteLine("Card dictionary is created.");

                return cards;
            });
        }
    }
}
