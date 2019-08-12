using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SabberStone_gRPC.MMF.Functions;

namespace SabberStone_gRPC.MMF
{
    public static class MMFServer
    {
        private const string PIPE_NAME_PREFIX = "sabberstoneserver_";
        private const string MMF_NAME_POSTFIX = "_sabberstoneserver.mmf";
        private const string OUTPUT_PATH = "_mmf_server_output.txt";

        public static List<Task> RunningThreads = new List<Task>();
        private static CancellationTokenSource CTS = new CancellationTokenSource();

        public static unsafe void Run(string id = "", bool isTask = false)
        {
            StreamWriter outputFile;
            TextWriter stdout = null;
            if (!isTask)
            {
                outputFile = File.CreateText(id + OUTPUT_PATH);
                outputFile.AutoFlush = true;
                stdout = Console.Out;
                Console.SetOut(outputFile);
            }
            
            while (true)
            {
                using (var pipe = new NamedPipeServerStream(PIPE_NAME_PREFIX + id, PipeDirection.InOut, 99))
                using (var mmf = MemoryMappedFile.CreateFromFile(
                    File.Open(Path.Combine("../", id + MMF_NAME_POSTFIX), FileMode.OpenOrCreate),
                    null, 10000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
                using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor())
                {
                    byte* mmfPtr = null;
                    view.SafeMemoryMappedViewHandle.AcquirePointer(ref mmfPtr);

                    Console.WriteLine("Server started. Waiting for the client.....");

                    pipe.WaitForConnection();

                    Console.WriteLine("Python client connected!");
                    try
                    {
                        using (BinaryWriter bw = new BinaryWriter(pipe))
                        using (BinaryReader br = new BinaryReader(pipe))
                        {
                            while (true)
                            {
                                byte function_id = br.ReadByte();

                                if (function_id == (byte)FunctionId.Terminate)
                                {
                                    CTS.Cancel();
                                    if (!isTask) Console.SetOut(stdout);
                                    return;
                                }
                                else if (function_id == (byte) FunctionId.NewThread)
                                {
                                    //int len = br.ReadInt32();
                                    //string threadId = Encoding.Default.GetString(br.ReadBytes(len));
                                    int threadId = br.ReadInt32();
                                    Console.WriteLine($"New Thread {threadId} is created.");
                                    RunningThreads.Add(
                                        Task.Factory.StartNew(() => 
                                            Run(id + threadId, true), CTS.Token));
                                    bw.Write((byte) 4);
                                    continue;
                                }


                                //Debug.WriteLine($"Function {function_id} is requested");

                                List<dynamic> arguments = new List<dynamic>();

                                while (true)
                                {
                                    char type = br.ReadChar();
                                    if (type == 'i')
                                        arguments.Add(br.ReadInt32());
                                    else if (type == 'b')
                                        arguments.Add(br.ReadBoolean());
                                    else if (type == 's')
                                    {
                                        int len = br.ReadInt32();
                                        string str = Encoding.Default.GetString(br.ReadBytes(len));
                                        arguments.Add(str);
                                    }
                                    else if (type == 'o')
                                    {
                                        var bytes = br.ReadBytes(Option.Size);
                                        unsafe
                                        {
                                            fixed (byte* ptr = bytes)
                                            {
                                                Option* opPtr = (Option*)ptr;
                                                arguments.Add(*opPtr);
                                            }
                                        }
                                    }
                                    else if (type == '4') // End of Transmission
                                        break;
                                    else
                                    {
                                        Console.WriteLine("Undefined value is received: " + type);
                                        break;
                                    }

                                }

                                try
                                {
                                    int size = FunctionTable.CallById((FunctionId) function_id, arguments, in mmfPtr);

                                    //Debug.WriteLine($"Server writes a structure of size {size} to mmf");

                                    bw.Write(size); // Send the size of returned structure.
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                    Console.WriteLine(e.StackTrace);
                                }
                            }
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("ERROR: " + e.Message);
                        Console.WriteLine("Connection closed.");
                    }
                }
            }
        }
    }
}
