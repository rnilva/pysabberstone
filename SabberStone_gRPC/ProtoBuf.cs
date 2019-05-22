using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using SabberStoneCore.Model;

namespace SabberStone_gRPC
{
    public static class ProtoBuf
    {
        public static void SerialiseCard()
        {
            string proto = Serializer.GetProto<Card>();
        }
    }
}
