using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace SabberStone_gRPC
{
    public static class FileLockTest
    {
        public const string LOCK_PATH = "../../../../test.lock";

        public class FileLock : IDisposable
        {
            private FileStream _handle;

            private FileLock(FileStream file)
            {
                _handle = file;
            }

            public void Release()
            {
                _handle.Close();
            }

            public static FileLock Lock()
            {
                while (true)
                {
                    try
                    {
                        FileLock fileLock = new FileLock(
                            File.Open(LOCK_PATH, 
                                FileMode.Open, 
                                FileAccess.ReadWrite, 
                                FileShare.None));

                        fileLock._handle.Lock(0, 0);
                        return fileLock;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(1);
                    }
                }
            }

            #region IDisposable

            public void Dispose()
            {
                _handle?.Dispose();
            }

            #endregion
        }

        public static void Test()
        {
            Console.WriteLine(Directory.GetCurrentDirectory());

            string path = "../../../../test.lock";

            Console.WriteLine("C# client is trying to get the exclusive lock...");

            bool flag = false;
            while (true)
            {
                try
                {
                    using (var file = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        file.Lock(0, 0);
                        Console.WriteLine("C# client finally gets the exclusive lock!");
                        Console.WriteLine("Press ENTER to release the lock");
                        Console.ReadLine();
                    }
                    break;
                }
                catch (IOException)
                {
                    if (flag)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    Console.WriteLine("Locked. Waiting...");
                    Thread.Sleep(1);
                    flag = true;
                }
            }

            Console.WriteLine("C# client releases the exclusive lock.");
        }
    }
}
