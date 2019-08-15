using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SabberStoneCore.Config;
using SabberStoneCore.Model;
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

    public readonly struct HandZone_unmanaged
    {
        public readonly int Count;
        public readonly IntPtr Playables;

        public HandZone_unmanaged(SabberZones.HandZone zone)
        {
            var span = zone.GetSpan();
            Count = span.Length;
            int size = Marshal.SizeOf<Playable>();
            IntPtr playables = Marshal.AllocHGlobal(size * Count);
            IntPtr ptr = playables;
            for (int i = 0; i < span.Length; i++)
            {
                Marshal.StructureToPtr(new Playable(span[i], true), ptr, false);
                ptr += size;
            }
            Playables = playables;
        }

        public void Free()
        {
            Marshal.FreeHGlobal(Playables);
        }

        public static void Test()
        {
            var game = new SabberStoneCore.Model.Game(new GameConfig
            {
                StartPlayer = 1,
                Shuffle = false,
                History = false,
                Logging = false,
                Player1Deck = new List<Card>
                {
                    Cards.FromName("Stonetusk Boar"),
                    Cards.FromName("Wisp"),
                    Cards.FromName("Bloodfen Raptor"),
                    Cards.FromName("Dalaran Mage")
                }
            });

            game.StartGame();

            var unmanagedHand = new HandZone_unmanaged(game.CurrentPlayer.HandZone);
            IntPtr ptr = unmanagedHand.Playables;
            for (int i = 0; i < unmanagedHand.Count; i++)
            {
                Playable playable = Marshal.PtrToStructure<Playable>(ptr);
                ptr += Marshal.SizeOf<Playable>();

                Console.WriteLine("- Card 1:");
                Console.WriteLine($"\t{Cards.FromAssetId(playable.CardId).Name}");
                Console.WriteLine($"\tCost: {playable.Cost}");
                Console.WriteLine($"\tATK: {playable.ATK}");
                Console.WriteLine($"\tHP: {playable.BaseHealth}");
            }

            unmanagedHand.Free();
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
