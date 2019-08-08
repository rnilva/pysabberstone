using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneCore.Model;
using static SabberStonePython.SabberHelpers;

namespace SabberStone_gRPC.MMF
{
    public static class SabberHelper
    {
        public static Game GenerateGame(string deckString1, string deckString2, bool startGame = true)
        {
            Deck deck1, deck2;

            try
            {
                deck1 = Deserialise(deckString1);
            } 
            catch (Exception e)
            {
                Console.WriteLine("Deckstring #1 is not a valid deckstring");
                throw e;
            }

            try
            {
                deck2 = Deserialise(deckString2);
            }
            catch (Exception e)
            {
                Console.WriteLine("Deckstring #2 is not a valid deckstring");
                throw e;
            }

            var game = new Game(new SabberStoneCore.Config.GameConfig
            {
                StartPlayer = -1,
                Player1HeroClass = deck1.Class,
                Player1Deck = deck1.GetCardList(),
                Player2HeroClass = deck2.Class,
                Player2Deck = deck2.GetCardList(),

                Logging = false,
                History = false,
                FillDecks = false,
                Shuffle = true,
                SkipMulligan = true,
            });

            if (startGame) game.StartGame();

            return game;
        }
    }
}
