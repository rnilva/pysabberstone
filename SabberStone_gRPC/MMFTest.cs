using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SabberStone_gRPC
{
    public static class MMFTest
    {
        private const string MMF_PATH = "../test.mmf";

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public readonly struct TestStructure
        {
            public static readonly int Size = Marshal.SizeOf<TestStructure>();

            public readonly int data1;
            public readonly int data2;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
            public readonly string data3;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 200)]
            public readonly string data4;

            public TestStructure(int d1, int d2, string d3, string d4)
            {
                data1 = d1;
                data2 = d2;
                data3 = d3;
                data4 = d4;
            }
        }

        public static void Test()
        {
            using (var mmf = MemoryMappedFile.CreateFromFile(
                File.Open(MMF_PATH, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite),
                "testmmf", 10000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
            {
                //Mutex mutex = new Mutex(true, "testmmfmutext", out bool mutexCreated);
                //var mutex = FileLockTest.FileLock.Lock();

                using (var mutex = FileLockTest.FileLock.Lock())
                {
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        BinaryWriter writer = new BinaryWriter(stream);
                        writer.Write(1);
                    }
                }

                Console.WriteLine("Start Python Process and press ENTER to continue.");
                Console.ReadLine();

                using (var mutex = FileLockTest.FileLock.Lock())
                {
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                    {
                        BinaryReader reader = new BinaryReader(stream);
                        Console.WriteLine("C# process says: {0}", reader.ReadBoolean());
                        Console.WriteLine("Python process says: {0}", reader.ReadBoolean());
                    }
                }
            }
        }

        public static MemoryMappedFile OpenMMF()
        {
            var mmf = MemoryMappedFile.CreateFromFile(MMF_PATH);

            using (var stream = mmf.CreateViewStream())
            {
                stream.Flush();
            }

            return mmf;
        }

        // public static void CloseMMF(MemoryMappedFile mmf)
        // {
        //     return mmf.
        // }

        public static unsafe void WriteStructure(MemoryMappedFile mmf, in TestStructure structure)
        {
            // using (var mutex = FileLockTest.FileLock.Lock())
            // {
            //     using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor())
            //     {
            //         byte* ptr = null;
            //         view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            //         IntPtr intPtr = (IntPtr)ptr;
            //         Marshal.StructureToPtr(structure, intPtr, false);
            //         Console.WriteLine("Structure is written in the mmf. size: " + TestStructure.Size);
            //     }
            // }
            using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor())
                {
                    byte* ptr = null;
                    view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                    Marshal.StructureToPtr(structure, (IntPtr)ptr, false);
                    Console.WriteLine("Structure is written in the mmf. size: " + TestStructure.Size);
                }
        }

        struct TestStruct
        {
            private int a;
            private int b;
            private int c;

            public TestStruct(int aa, int bb, int cc)
            {
                a = aa;
                b = bb;
                c = cc;
            }
        }

        public static unsafe void MarshalTest()
        {
            
            //IntPtr intPtr = new IntPtr();

            

            using (var mmf = MemoryMappedFile.CreateFromFile(
                File.Open(MMF_PATH, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite),
                "testmmf", 10000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
            {
                using (var view = mmf.CreateViewAccessor())
                {
                    TestStruct structure = new TestStruct(1, 2, 3);
                    byte* ptr = null;
                    view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                    Marshal.StructureToPtr<TestStruct>(structure, (IntPtr)ptr, false);
                    //Buffer.MemoryCopy((void*)intPtr, ptr, sizeof(TestStruct), sizeof(TestStruct));
                    Console.WriteLine("Struct is written");
                }

                Console.ReadLine();
            }
        }
    }


}
