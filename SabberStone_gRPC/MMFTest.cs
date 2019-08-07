using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SabberStone_gRPC
{
    public class MMFTest
    {
        private const string MMF_PATH = "../../../../test.mmf";

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
