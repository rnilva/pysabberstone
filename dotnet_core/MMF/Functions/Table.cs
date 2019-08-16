using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using SabberStoneCore.Config;
using SabberStoneCore.Model;
using MMFEntities = SabberStone_gRPC.MMF.Entities;

namespace SabberStone_gRPC.MMF.Functions
{
    public enum FunctionId : byte
    {
        Test = 0,
        TestMultiArgument = 1,
        TestSendOnePlayable = 2,
        TestSendZoneWithPlayables = 3,

        NewGame = 4,
        Reset = 5,
        Options = 6,
        Process = 7,
        
        Terminate = 8,
        NewThread = 9,

        Status = 10
    }

    public static unsafe class FunctionTable
    {
        public static Dictionary<FunctionId, Stopwatch> Watches = new Dictionary<FunctionId, Stopwatch>
        {
            [FunctionId.Process] = new Stopwatch(),
            [FunctionId.Options] = new Stopwatch(),
            [FunctionId.Reset] = new Stopwatch()
        };

        public static int CallById(FunctionId id, List<dynamic> arguments, in byte* mmfPtr)
        {
            Stopwatch watch;
            if (Watches.TryGetValue(id, out watch))
                watch.Start();
            
            int value = 0;

            try
            {
                switch (id)
                {
                    case FunctionId.Test:
                        Test();
                        break;
                    case FunctionId.TestMultiArgument:
                        TestMultiArgument(arguments[0], arguments[1], arguments[2]);
                        break;
                    case FunctionId.TestSendOnePlayable:
                        value = TestSendOnePlayable(in mmfPtr);
                        break;
                    case FunctionId.TestSendZoneWithPlayables:
                        value =  TestSendZoneWithPlayables(in mmfPtr);
                        break;
                    case FunctionId.NewGame:
                        value =  API.NewGame((string)arguments[0], (string)arguments[1], in mmfPtr);
                        break;
                    case FunctionId.Reset:
                        value =  API.Reset((int)arguments[0], in mmfPtr);
                        break;
                    case FunctionId.Options:
                        value =  API.GetOptions((int)arguments[0], in mmfPtr);
                        break;
                    case FunctionId.Process:
                        value =  API.Process((int)arguments[0], (Option)arguments[1], in mmfPtr);
                        break;
                    case FunctionId.Terminate:
                        Environment.Exit(1);
                        break;
                    case FunctionId.Status:
                        value =  API.Status(in mmfPtr);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid arguments for function " + id);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return -1;
            }

            watch?.Stop();

            return value;
        }

        public static void Test()
        {

        }

        public static void TestMultiArgument(int a, int b, bool c)
        {

        }

        public static int TestSendOnePlayable(in byte* mmfPtr)
        {
            var playable = new MMFEntities.Playable(1, 2, 3, 4, true);
            return WriteStructure(in mmfPtr, in playable);
        }

        public static int TestSendZoneWithPlayables(in byte* mmfPtr)
        {
            var game = new Game(new GameConfig
            {
                StartPlayer = 1,
                Shuffle = false,
                History = false,
                Logging = false,
                Player1Deck = new List<Card>
                {
                    Cards.FromName("Stonetusk Boar"),
                    Cards.FromName("Wisp"),
                    Cards.FromName("Bloodfen Raptor"),
                    Cards.FromName("Dalaran Mage")
                }
            });

            game.StartGame();

            int* ip = MarshalEntities.MarshalHandZone(game.CurrentPlayer.HandZone, (int*) mmfPtr);
            return (int) ((byte*)ip - mmfPtr);
        }

        private static int WriteStructure<T>(in byte* mmfPtr, in T structure) where T : struct
        {
            Marshal.StructureToPtr(structure, (IntPtr) mmfPtr, false);

            return Marshal.SizeOf<T>();
        }
    }
}
