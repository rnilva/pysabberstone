using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using SabberStoneCore.Model;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStone_gRPC.MMF.Functions
{
    public static class API
    {
        private static class ManagedObjects
        {
            public static Dictionary<int, Game> Games = new Dictionary<int, Game>();
            public static Dictionary<int, Game> InitialGames = new Dictionary<int, Game>();
            public static Dictionary<int, byte[]> InitialGameAPIs = new Dictionary<int, byte[]>();
            public static Dictionary<int, List<SabberStonePython.API.Option>> OptionBuffers = new Dictionary<int, List<SabberStonePython.API.Option>>();
        }

        private static int _gameIdGen;

        public static unsafe int NewGame(string deck1, string deck2, MemoryMappedFile mmf)
        {
            Console.WriteLine("NewGame service is called!");
            Console.WriteLine("Deckstring #1: " + deck1);
            Console.WriteLine("Deckstring #2: " + deck2);

            Game game = SabberHelper.GenerateGame(deck1, deck2);
            int id = _gameIdGen++;

            ManagedObjects.InitialGames.Add(id, game);
            ManagedObjects.Games.Add(id, game);

            int size = MarshalEntities.MarshalGameToMMF(game, mmf, id);

            Console.WriteLine($"New Game of size {size} is created");

            using (var view = mmf.CreateViewAccessor())
            {
                byte* sourcePtr = null;
                view.SafeMemoryMappedViewHandle.AcquirePointer(ref sourcePtr);

                var destinationArray = new byte[size];
                fixed (byte* dstPtr = destinationArray)
                    Buffer.MemoryCopy(sourcePtr, dstPtr, size, size);

                ManagedObjects.InitialGameAPIs.Add(id, destinationArray);
            }

            return size;
        }

        public static unsafe int Reset(int gameId, MemoryMappedFile mmf)
        {
            ManagedObjects.Games[gameId] = ManagedObjects.InitialGames[gameId].Clone();

            using (var view = mmf.CreateViewAccessor())
            {
                byte* dstPtr = null;
                view.SafeMemoryMappedViewHandle.AcquirePointer(ref dstPtr);

                byte[] source = ManagedObjects.InitialGameAPIs[gameId];
                int size = source.Length;
                fixed (byte* sourcePtr = source)
                    Buffer.MemoryCopy(sourcePtr, dstPtr, size, size);

                return size;
            }
        }

        public static int GetOptions(int gameId, MemoryMappedFile mmf)
        {
            return ManagedObjects.Games[gameId].CurrentPlayer.MarshalOptions(mmf);
        }

        public static int Process(int gameId, in Option option, MemoryMappedFile mmf)
        {
            Game game = ManagedObjects.Games[gameId];
            PlayerTask task = SabberHelper.OptionToPlayerTask(game.CurrentPlayer, option);
            game.Process(task);

            return MarshalEntities.MarshalGameToMMF(game, mmf, gameId);
        }
    }
}
