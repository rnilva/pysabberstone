using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;


namespace SabberStone_gRPC
{
    public class NamedPipeTest
    {
        public static void Test()
        {
            const string pipeName = "testpipe";

            Console.WriteLine(System.Environment.CurrentDirectory);

            var npss = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1);
            var mmf = MMFTest.OpenMMF();

            Console.WriteLine("NamedPipeServerStream object created. name: " + pipeName);

            npss.WaitForConnection();

            Console.WriteLine("Python client connected!");

            try
            {
                using (BinaryWriter bw = new BinaryWriter(npss))
                using (BinaryReader br = new BinaryReader(npss))
                {
                    while (true)
                    {
                        // Console.Write("Enter text: ");
                        // var str = Console.ReadLine();

                        // var buf = Encoding.ASCII.GetBytes(str);
                        // bw.Write((uint) buf.Length);
                        // bw.Write(buf);
                        // Console.WriteLine("Wrote: \"{0}\"", str);
                        Console.WriteLine("Let's hear from the client now...");

                        var function_id = br.ReadByte();
                        var arg = br.ReadInt32();
                        Console.WriteLine($"Function call Received! id: {function_id}, arg: {arg}");

                        var structure = new MMFTest.TestStructure(function_id, arg, $"function {function_id} returned", "this is rubbish");

                        MMFTest.WriteStructure(mmf, in structure);

                        bw.Write(-1); // Send -1 that represents the end of function call.
                        bw.Write(MMFTest.TestStructure.Size); // Send the size of written structure.
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("ERROR: " + e.Message);
            }

            npss.Close();
        }
    }
}