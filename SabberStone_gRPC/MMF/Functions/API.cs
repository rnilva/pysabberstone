using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace SabberStone_gRPC.MMF.Functions
{
    public static class API
    {
        private static int _gameIdGen;

        public static void NewGame(MemoryMappedFile mmf, string deck1, string deck2)
        {
            Console.WriteLine("NewGame service is called!");
            Console.WriteLine("Deckstring #1: " + deck1);
            Console.WriteLine("Deckstring #2: " + deck2);

            var game = SabberHelper.GenerateGame(deck1, deck2);

            MarshalEntities.MarshalGameToMMF(game, mmf, _gameIdGen++);
        }
    }
}
