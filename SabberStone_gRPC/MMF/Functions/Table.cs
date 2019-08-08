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
    public static class FunctionTable
    {
        public static int CallById(FunctionId id, List<dynamic> arguments, MemoryMappedFile mmf)
        {
            try
            {
                switch (id)
                {
                    case FunctionId.Test:
                        Test();
                        break;
                    case FunctionId.Test_MultiArgument:
                        TestMultiArgument(arguments[0], arguments[1], arguments[2]);
                        break;
                    case FunctionId.Test_SendOnePlayable:
                        return TestSendOnePlayable(mmf);
                    case FunctionId.Test_SendZoneWithPlayables:
                        return TestSendZoneWithPlayables(mmf);
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("Invalid arguments for function " + id);
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

        public static int TestSendOnePlayable(MemoryMappedFile mmf)
        {
            var playable = new MMFEntities.Playable(1, 2, 3, 4, true);
            return WriteStructure(mmf, in playable);
        }

        public static int TestSendZoneWithPlayables(MemoryMappedFile mmf)
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

            //return WriteStructure(mmf, new MMFEntities.HandZone(game.CurrentPlayer.HandZone));

            using (var view = mmf.CreateViewAccessor())
            {
                unsafe
                {
                    byte* ptr = null;
                    view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                    MarshalEntities.MarshalHandZone(game.CurrentPlayer.HandZone, (int*) ptr);
                }
            }

            return game.CurrentPlayer.HandZone.Count * MMFEntities.Playable.Size + 4;
        }

        private static unsafe int WriteStructure<T>(MemoryMappedFile mmf, in T structure) where T : struct
        {
            using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor())
            {
                byte* ptr = null;
                view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                Marshal.StructureToPtr(structure, (IntPtr) ptr, false);
            }

            return Marshal.SizeOf<T>();
        }
    }

    public enum FunctionId : byte
    {
        Test = 0,
        Test_MultiArgument = 1,
        Test_SendOnePlayable = 2,

        Test_SendZoneWithPlayables = 3
    }


}
