using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
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

    public partial class Cards
    {
        public Cards(IEnumerable<SabberStoneCore.Model.Card> allCards)
        {
            //var pairs = new RepeatedField<Cards.Types.Pair>();
            //pairs.AddRange(allCards
            //    .Select(c => new Types.Pair
            //    {
            //        Id = c.AssetId,
            //        Card = new Card
            //        {
            //            Id = c.AssetId,
            //            Name = c.Name,
            //            StringId = c.Id,
            //        }
            //    }));
            cards_ = new MapField<int, Card>();

            try
            {
                foreach (var card in allCards)
                {
                    if (card.Name == null)
                        continue;

                    cards_.Add(card.AssetId, new Card
                    {
                        Id = card.AssetId,
                        Name = card.Name,
                        StringId = card.Id
                    });
                }
            }
            catch (Exception e)
            {
                ;
            }


            ;
        }
    }
}
