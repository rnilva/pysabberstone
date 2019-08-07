using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
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
                        return SendOnePlayable(mmf);
                    default:
                        throw new NotImplementedException();
                }
            }
            catch (ArgumentOutOfRangeException e)
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

        public static int SendOnePlayable(MemoryMappedFile mmf)
        {
            var playable = new MMFEntities.Playable();
            return WriteStructure(mmf, in playable);
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
    }


}
