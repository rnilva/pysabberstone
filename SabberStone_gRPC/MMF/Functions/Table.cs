using System;
using System.Collections.Generic;
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
        NewThread = 9
    }

    public static unsafe class FunctionTable
    {
        public static int CallById(FunctionId id, List<dynamic> arguments, in byte* mmfPtr)
        {
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
                        return TestSendOnePlayable(in mmfPtr);
                    case FunctionId.TestSendZoneWithPlayables:
                        return TestSendZoneWithPlayables(in mmfPtr);
                    case FunctionId.NewGame:
                        return API.NewGame((string)arguments[0], (string)arguments[1], in mmfPtr);
                    case FunctionId.Reset:
                        return API.Reset((int)arguments[0], in mmfPtr);
                    case FunctionId.Options:
                        return API.GetOptions((int)arguments[0], in mmfPtr);
                    case FunctionId.Process:
                        return API.Process((int)arguments[0], (Option)arguments[1], in mmfPtr);
                    case FunctionId.Terminate:
                        Environment.Exit(1);
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

            return 0;
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

            MarshalEntities.MarshalHandZone(game.CurrentPlayer.HandZone, (int*) mmfPtr, out int count);
            return 4 + count * MMFEntities.Playable.Size;
        }

        private static int WriteStructure<T>(in byte* mmfPtr, in T structure) where T : struct
        {
            Marshal.StructureToPtr(structure, (IntPtr) mmfPtr, false);

            return Marshal.SizeOf<T>();
        }
    }
}
