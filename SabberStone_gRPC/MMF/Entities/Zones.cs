using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SabberEntities = SabberStoneCore.Model.Entities;
using SabberZones = SabberStoneCore.Model.Zones;

namespace SabberStone_gRPC.MMF.Entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public readonly struct HandZone
    {
        public readonly int Count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly Playable[] Playables;

        public HandZone(SabberZones.HandZone handZone)
        {
            var playables = new Playable[10];
            var span = handZone.GetSpan();
            for (int i = 0; i < span.Length; i++)
                playables[i] = new Playable(span[i], true);
            
            Count = span.Length;
            Playables = playables;
        }
    }
}
