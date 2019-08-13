using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using MMFEntities = SabberStone_gRPC.MMF.Entities;

namespace SabberStone_gRPC.MMF
{
    public static class PythonHelper
    {
        private const string DEFAULT_FILE_OUTPUT_PATH = "./";
        private const string PYTHON_ENTITIES_FILE_NAME = "entities.py";
        private const string PYTHON_ZONES_FILE_NAME = "zones.py";

        public static readonly Type[] EntityTypes =
        {
            typeof(MMFEntities.Playable),
            typeof(MMFEntities.HeroPower),
            typeof(MMFEntities.Weapon),
            typeof(MMFEntities.Minion),
            typeof(Option)
            //typeof(MMFEntities.Hero)
        };

        public static readonly Type[] ZoneTypes =
        {
            typeof(MMFEntities.HandZone)
        };

        public static void WritePythonEntities(string path = DEFAULT_FILE_OUTPUT_PATH + PYTHON_ENTITIES_FILE_NAME)
        {
            var file = File.Open(path, FileMode.OpenOrCreate);
            var writer = new StreamWriter(file);
            writer.WriteLine("# automatically generated source");
            writer.WriteLine("# SabberStoneServer entities");
            writer.WriteLine("from struct import *");
            writer.WriteLine("import numpy as np");
            writer.WriteLine();

            foreach (Type entityType in EntityTypes)
            {
                writer.WriteLine();
                StringBuilder fmtBuilder = new StringBuilder();
                StringBuilder initialiser = new StringBuilder("        (\n");

                writer.WriteLine($"class {entityType.Name}:");
                FieldInfo[] fields = entityType.GetFields();
                // List<string> names = new List<string>();
                // foreach (FieldInfo field in fields)
                // {
                //     switch (field.FieldType.Name)
                //     {
                //         case "Int32":
                //             fmtBuilder.Append('i');
                //             break;
                //         case "Boolean":
                //             fmtBuilder.Append('?');
                //             break;
                //         default:
                //             break;
                //     }

                //     string self_member = new string(' ', 12) + "self." + field.Name.ToUnderScoreSnake();
                //     names.Add(self_member);
                // }

                // initialiser.AppendJoin(",\n", names.ToArray());
                // initialiser.AppendLine();
                // initialiser.AppendLine("        ) = fields");

                // writer.WriteLine($"    fmt = \'{fmtBuilder}\'");
                // writer.WriteLine(
                //     $"    size = {typeof(Marshal).GetMethod("SizeOf", new Type[]{}).MakeGenericMethod(entityType).Invoke(null, null)}");
                // writer.WriteLine();
                // writer.WriteLine("    def __init__(self, data_bytes):");
                // writer.WriteLine($"        fields = unpack({entityType.Name}.fmt, data_bytes)");
                // writer.WriteLine(initialiser);
                writer.WriteLine("dtype = np.dtype([".Indent(1));
                List<string> fieldStrings = new List<string>();
                foreach (FieldInfo field in fields)
                {
                    if (field.IsStatic) continue;

                    var sb = new StringBuilder();
                    sb.Append($"(\'{field.Name.ToUnderScoreSnake()}\', '".Indent(2));
                    switch (field.FieldType.Name)
                    {
                        case "Int32":
                            sb.Append("i");
                            break;
                        case "Boolean":
                            sb.Append("b");
                            break;
                        default:
                            break;
                    }
                    sb.Append("\')");
                    fieldStrings.Add(sb.ToString());
               }
               writer.WriteLine(string.Join(",\n", fieldStrings.ToArray()));
               writer.WriteLine("])".Indent(1));
               writer.WriteLine("size = dtype.itemsize".Indent(1));
            }

            writer.Close();
            file.Close();
        }

        public static void WritePythonZones(string path = DEFAULT_FILE_OUTPUT_PATH + PYTHON_ZONES_FILE_NAME)
        {
            // var file = File.OpenWrite(path);
            var writer = File.CreateText(path);
            writer.WriteLine("# automatically generated source");
            writer.WriteLine("# SabberStoneServer entities");
            writer.WriteLine("from struct import *");
            writer.WriteLine();
            writer.WriteLine();
        }

        private static string ToUnderScoreSnake(this string str)
        {
            if (str.All(char.IsUpper))
                return str.ToLower();

            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        private static string Indent(this string str, int count)
        {
            return new string(' ', count * 4) + str;
        }
    }
}
