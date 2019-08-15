using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using SabberStoneCore.Model;
using Google.Protobuf.Collections;

namespace SabberStone_gRPC
{
    public static class StaticTest
    {
        public static Game Game;
    }

    public class SabberStoneRPCImpl : SabberStoneRPC.SabberStoneRPCBase
    {
        //private Game Game;

        public override Task<Empty> CreateGame(Empty request, ServerCallContext context)
        {
            return Task<Empty>.Factory.StartNew(() =>
            {
                StaticTest.Game = new Game(new SabberStoneCore.Config.GameConfig
                {
                    Logging = true,
                    History = false,
                    FillDecks = true,
                    Player1HeroClass = SabberStoneCore.Enums.CardClass.MAGE,
                    Player2HeroClass = SabberStoneCore.Enums.CardClass.HUNTER
                });

                StaticTest.Game.StartGame();

                Console.WriteLine("Game Created!");

                return new Empty();
            });
        }

        public override Task<HandZone> CurrentHand(Empty request, ServerCallContext context)
        {
            return Task.FromResult(GetCurrentHand());
        }

        private HandZone GetCurrentHand()
        {
            var sabberHand = StaticTest.Game.CurrentPlayer.HandZone.GetSpan();

            var hand = new HandZone
            {
                Count = sabberHand.Length,
            };

            var playables = hand.Playables;
            for (int i = 0; i < sabberHand.Length; i++)
            {
                var playable = sabberHand[i];

                playables.Add(new Playable
                {
                    CardId = playable.Card.AssetId,
                    Cost = playable.Cost,
                    Echo = playable.IsEcho,
                    Id = playable.Id
                });
            }

            return hand;
        }
    }
}
