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

        public HandZone(SabberZones.HandZone zone)
        {
            var playables = new Playable[10];
            var span = zone.GetSpan();
            for (int i = 0; i < span.Length; i++)
                playables[i] = new Playable(span[i], true);
            
            Count = span.Length;
            Playables = playables;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public readonly struct BoardZone
    {
        public readonly int Count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public readonly Minion[] Playables;

        public BoardZone(SabberZones.BoardZone zone)
        {
            var playables = new Minion[7];
            var span = zone.GetSpan();
            for (int i = 0; i < span.Length; i++)
                playables[i] = new Minion(span[i]);
            
            Count = span.Length;
            Playables = playables;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public readonly struct SecretZone
    {
        public readonly int Count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly Playable[] Playables;

        public SecretZone(SabberZones.SecretZone zone)
        {
            var playables = new Playable[10];
            var span = zone.GetSpan();
            for (int i = 0; i < span.Length; i++)
                playables[i] = new Playable(span[i], true);
            
            Count = span.Length;
            Playables = playables;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public readonly struct DeckZone
    {
        public readonly int Count;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly Playable[] Playables;

        public DeckZone(SabberZones.DeckZone zone)
        {
            var playables = new Playable[10];
            var span = zone.GetSpan();
            for (int i = 0; i < span.Length; i++)
                playables[i] = new Playable(span[i], true);
            
            Count = span.Length;
            Playables = playables;
        }
    }
}
