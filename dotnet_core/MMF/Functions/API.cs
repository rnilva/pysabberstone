using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using SabberStoneCore.Model;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneCore.Enums;

namespace SabberStone_gRPC.MMF.Functions
{
    public static unsafe class API
    {
        private static class ManagedObjects
        {
            public static ConcurrentDictionary<int, Game> Games = new ConcurrentDictionary<int, Game>();
            public static ConcurrentDictionary<int, Game> InitialGames = new ConcurrentDictionary<int, Game>();
            // public static Dictionary<int, byte[]> InitialGameAPIs = new Dictionary<int, byte[]>();
        }

        private static object _locker = new object();
        private static int _gameIdGen;

        private static long _total_processing_time;
        private static int _num_games_played;

        private static int GetNextId()
        {
            lock (_locker)
            {
                return _gameIdGen++;
            }
        }

        public static int NewGame(string deck1, string deck2, in byte* mmfPtr)
        {
            if (_num_games_played > 0)
                Console.WriteLine("Avg processing time: " + _total_processing_time / _num_games_played + " ms");

            ++_num_games_played;

            Console.WriteLine("NewGame service is called!");
            Console.WriteLine("Deckstring #1: " + deck1);
            Console.WriteLine("Deckstring #2: " + deck2);

            Game game = SabberHelper.GenerateGame(deck1, deck2, false);
            int id = GetNextId();

            ManagedObjects.InitialGames.TryAdd(id, game.Clone());
            ManagedObjects.Games.TryAdd(id, game);
            game.StartGame();

            int size = MarshalEntities.MarshalGameToMMF(game, in mmfPtr, id);

            Console.WriteLine($"New Game of size {size} is created");

            // var destinationArray = new byte[size];
            // fixed (byte* dstPtr = destinationArray)
            //     Buffer.MemoryCopy(mmfPtr, dstPtr, size, size);

            // ManagedObjects.InitialGameAPIs.Add(id, destinationArray);

            return size;
        }

        public static int Reset(int gameId, in byte* mmfPtr)
        {
            // ManagedObjects.Games[gameId] = ManagedObjects.InitialGames[gameId].Clone();

            // byte[] source = ManagedObjects.InitialGameAPIs[gameId];
            // int size = source.Length;
            // fixed (byte* sourcePtr = source)
            //     Buffer.MemoryCopy(sourcePtr, mmfPtr, size, size);

            Game game = ManagedObjects.InitialGames[gameId].Clone();
            ManagedObjects.Games[gameId] = game;
            game.StartGame();
            return MarshalEntities.MarshalGameToMMF(game, in mmfPtr, gameId);
        }

        public static int GetOptions(int gameId, in byte* mmfPtr)
        {
            var watch = Stopwatch.StartNew();

            var result =  ManagedObjects.Games[gameId].CurrentPlayer.Options(in mmfPtr);

            watch.Stop();
            _total_processing_time += watch.ElapsedMilliseconds;
            
            return result;
        }

        public static int Process(int gameId, in Option option, in byte* mmfPtr)
        {
            var watch = Stopwatch.StartNew();

            Game game = ManagedObjects.Games[gameId];
            PlayerTask task = SabberHelper.OptionToPlayerTask(game.CurrentPlayer, option);
            game.Process(task);

            var result =  MarshalEntities.MarshalGameToMMF(game, in mmfPtr, gameId);
            watch.Stop();
            _total_processing_time += watch.ElapsedMilliseconds;
            return result;
        }

        public static unsafe int Status(in byte* mmfPtr)
        {
            //var sb = new StringBuilder();

            var str = $"Server status: {MMFServer.RunningThreads.Count + 1} Threads, Total {ManagedObjects.Games.Count} Games, {ManagedObjects.Games.Count(p => p.Value.State == State.COMPLETE)} Completed Games.";
            fixed (char* ptr = str)
            {
                Encoding.Default.GetBytes(ptr, str.Length, mmfPtr, 1000);
            }
            
            return str.Length;
        }
    }
}
