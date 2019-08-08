using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using SabberStoneCore.Model;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStone_gRPC.MMF.Functions
{
    public static unsafe class API
    {
        private static class ManagedObjects
        {
            public static Dictionary<int, Game> Games = new Dictionary<int, Game>();
            public static Dictionary<int, Game> InitialGames = new Dictionary<int, Game>();
            public static Dictionary<int, byte[]> InitialGameAPIs = new Dictionary<int, byte[]>();
            public static Dictionary<int, List<SabberStonePython.API.Option>> OptionBuffers = new Dictionary<int, List<SabberStonePython.API.Option>>();
        }

        private static int _gameIdGen;

        public static int NewGame(string deck1, string deck2, in byte* mmfPtr)
        {
            Console.WriteLine("NewGame service is called!");
            Console.WriteLine("Deckstring #1: " + deck1);
            Console.WriteLine("Deckstring #2: " + deck2);

            Game game = SabberHelper.GenerateGame(deck1, deck2);
            int id = _gameIdGen++;

            ManagedObjects.InitialGames.Add(id, game);
            ManagedObjects.Games.Add(id, game);

            int size = MarshalEntities.MarshalGameToMMF(game, in mmfPtr, id);

            Console.WriteLine($"New Game of size {size} is created");

            var destinationArray = new byte[size];
            fixed (byte* dstPtr = destinationArray)
                Buffer.MemoryCopy(mmfPtr, dstPtr, size, size);

            ManagedObjects.InitialGameAPIs.Add(id, destinationArray);
            
            return size;
        }

        public static int Reset(int gameId, in byte* mmfPtr)
        {
            ManagedObjects.Games[gameId] = ManagedObjects.InitialGames[gameId].Clone();

            byte[] source = ManagedObjects.InitialGameAPIs[gameId];
            int size = source.Length;
            fixed (byte* sourcePtr = source)
                Buffer.MemoryCopy(sourcePtr, mmfPtr, size, size);

            return size;
        }

        public static int GetOptions(int gameId, in byte* mmfPtr)
        {
            return ManagedObjects.Games[gameId].CurrentPlayer.Options(in mmfPtr);
        }

        public static int Process(int gameId, in Option option, in byte* mmfPtr)
        {
            Game game = ManagedObjects.Games[gameId];
            PlayerTask task = SabberHelper.OptionToPlayerTask(game.CurrentPlayer, option);
            game.Process(task);

            return MarshalEntities.MarshalGameToMMF(game, in mmfPtr, gameId);
        }
    }
}
