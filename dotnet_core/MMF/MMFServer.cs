using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SabberStoneCore;
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

        public static string GetTempPath(string id) => Path.GetTempFileName() + Guid.NewGuid().ToString() + id + ".mmf";

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

            string mmf_file_path = GetTempPath(id);

            var watchArguments = new Stopwatch();
            var watchGetOption = new Stopwatch();
            var watchFunctionCall = new Stopwatch();
            var watchSenSsize = new Stopwatch();

            while (true)
            {
                using (var pipe = new NamedPipeServerStream(PIPE_NAME_PREFIX + id, PipeDirection.InOut, 1))
                using (var mmf = MemoryMappedFile.CreateFromFile(
                    File.Open(mmf_file_path, FileMode.OpenOrCreate),
                    null, 10000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
                using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor())
                {
                    byte* mmfPtr = null;
                    view.SafeMemoryMappedViewHandle.AcquirePointer(ref mmfPtr);

                    Console.WriteLine($"Server({id}) started. Waiting for the client.....");

                    Task.Run(() => 
                        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(SabberStoneCore.Model.Cards).TypeHandle));

                    pipe.WaitForConnection();

                    Console.WriteLine("Python client connected!");

                    Span<byte> localBuffer = stackalloc byte[1000];
                    try
                    {
                        using (BinaryWriter bw = new BinaryWriter(pipe))
                        using (BinaryReader br = new BinaryReader(pipe))
                        {
                            bw.Write(Encoding.Default.GetBytes(mmf_file_path));
                            while (true)
                            {
                                pipe.Read(localBuffer);

                                //byte function_id = br.ReadByte();
                                byte function_id = localBuffer[0];

                                watchArguments.Start();
                                if (function_id == (byte)FunctionId.Terminate)
                                {
                                    watchArguments.Stop();
                                    Console.WriteLine("Arguments " + watchArguments.ElapsedMilliseconds + " ms");
                                    Console.WriteLine("Option parsing " + watchGetOption.ElapsedMilliseconds + " ms");
                                    Console.WriteLine("Functions " + watchFunctionCall.ElapsedMilliseconds + " ms");
                                    Console.WriteLine("Writing " + watchSenSsize.ElapsedMilliseconds + " ms");
                                    foreach (var pair in FunctionTable.Watches)
                                    {
                                        Console.WriteLine($"{pair.Key} {pair.Value.ElapsedMilliseconds} ms");
                                    }
                                    Console.WriteLine("Server({id}) is terminated.");

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
                                
                                List<dynamic> arguments = new List<dynamic>();

                                int offset = 1;
                                while (true)
                                {
                                    //char type = br.ReadChar();
                                    char type = (char)localBuffer[offset++];
                                    if (type == 'i')
                                    {
                                        var bytes = localBuffer.Slice(offset, 4);
                                        offset += 4;
                                        arguments.Add(BitConverter.ToInt32(bytes));
                                    }
                                    else if (type == 'b')
                                        arguments.Add(br.ReadBoolean());
                                    else if (type == 's')
                                    {
                                        // int len = br.ReadInt32();
                                        var bytes = localBuffer.Slice(offset, 4);
                                        offset += 4;
                                        int len = BitConverter.ToInt32(bytes);
                                        bytes = localBuffer.Slice(offset, len);
                                        offset += len;
                                        // string str = Encoding.Default.GetString(br.ReadBytes(len));
                                        string str = Encoding.Default.GetString(bytes);
                                        arguments.Add(str);
                                    }
                                    else if (type == 'o')
                                    {
                                        watchGetOption.Start();
                                        // var bytes = br.ReadBytes(Option.Size);
                                        var bytes = localBuffer.Slice(offset, Option.Size);
                                        offset += Option.Size;
                                        unsafe
                                        {
                                            fixed (byte* ptr = bytes)
                                            {
                                                Option* opPtr = (Option*)ptr;
                                                arguments.Add(*opPtr);
                                            }
                                        }
                                        watchGetOption.Stop();
                                    }
                                    else if (type == '4') // End of Transmission
                                        break;
                                    else
                                    {
                                        Console.WriteLine("Undefined value is received: " + type);
                                        break;
                                    }
                                }

                                watchArguments.Stop();
                                watchFunctionCall.Start();
                                try
                                {
                                    int size = FunctionTable.CallById((FunctionId) function_id, arguments, in mmfPtr);

                                    watchFunctionCall.Stop();
                                    watchSenSsize.Start();
                                    
                                    pipe.Write(BitConverter.GetBytes(size), 0, 4);
                                    // bw.Write(size); // Send the size of returned structure.
                                    watchSenSsize.Stop();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                    Console.WriteLine(e.StackTrace);
                                }
                                localBuffer.Clear();
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
