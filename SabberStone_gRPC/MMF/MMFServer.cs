using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Text;
using SabberStone_gRPC.MMF.Functions;

namespace SabberStone_gRPC.MMF
{
    public static class MMFServer
    {
        private const string PIPE_NAME_PREFIX = "sabberstoneserver_";
        private const string MMF_NAME_POSTFIX = "_sabberstoneserver.mmf";

        public static void Run(string id = "")
        {
            using (var pipe = new NamedPipeServerStream(PIPE_NAME_PREFIX + id, PipeDirection.InOut, 1))
            using (var mmf = MemoryMappedFile.CreateFromFile(
                File.Open(Path.Combine("../", id + MMF_NAME_POSTFIX), FileMode.OpenOrCreate),
                null, 10000, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, false))
            {
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

                            Console.WriteLine($"Function {function_id} is requested");

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
                                    string str = System.Text.Encoding.Default.GetString(br.ReadBytes(len));
                                    Console.WriteLine("received str: " + str);
                                    arguments.Add(str);
                                }
                                else if (type == '4')   // End of Transmission
                                    break;
                                else
                                {
                                    Console.WriteLine("Undefined value is received: " + type);
                                    break;
                                }
                                    
                            }

                            int size = FunctionTable.CallById((FunctionId) function_id, arguments, mmf);

                            Console.WriteLine($"Server writes a structure of size {size} to mmf");

                            bw.Write(size); // Send the size of returned structure.
                        }
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
            }
        }
    }
}
