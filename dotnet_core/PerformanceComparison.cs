using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SabberStone_gRPC.MMF;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberEntities = SabberStoneCore.Model.Entities;
using MMFEntities = SabberStone_gRPC.MMF.Entities;
using SabberGame = SabberStoneCore.Model.Game;

namespace SabberStone_gRPC
{
    public static class TestHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SabberGame GetTestGame()
        {
            var game = new SabberGame(new GameConfig
            {
                FillDecks = false,
                History = false,
                Logging = false,
                Shuffle = false
            });
            game.StartGame();
            return game;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SabberEntities.Controller GetTestController()
        {
            return GetTestGame().CurrentPlayer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SabberEntities.Minion GetTestMinion(Card card)
        {
            return (SabberEntities.Minion)SabberEntities.Entity.FromCard(GetTestController(), in card);
        }
    }

    public static class PerformanceComparison
    {
        public static unsafe void MarshalEntity()
        {
            const int COUNT = 10_000_000;

            var watch1 = new Stopwatch();
            var watch2 = new Stopwatch();
            var watch3 = new Stopwatch();
            var watch4 = new Stopwatch();

            var minion = TestHelper.GetTestMinion(Cards.FromName("Zilliax"));
            var heroPower = minion.Controller.Hero.HeroPower;

            var file = File.Open("./test.mmf", FileMode.Create, 
                FileAccess.ReadWrite, FileShare.ReadWrite);
            using (var mmf = MemoryMappedFile.CreateFromFile(file, null, 
                10000, MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None, false))
            using (var view = mmf.CreateViewAccessor())
            {
                byte* ptr = null;
                view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                watch1.Start();
                for (int i = 0; i < COUNT; i++)
                {
                    byte* start = ptr;
                    MarshalEntities.MarshalMinion(minion, start);
                }
                watch1.Stop();
                var result1 = Marshal.PtrToStructure<MMFEntities.Minion>((IntPtr) ptr);

                watch2.Start();
                for (int i = 0; i < COUNT; i++)
                    MarshalEntities.MarshalMinionPtr(minion, (int*) ptr);
                watch2.Stop();
                var result2 = Marshal.PtrToStructure<MMFEntities.Minion>((IntPtr) ptr);

                if (result1 != result2)
                    throw new Exception();

                watch3.Start();
                for (int i = 0; i < COUNT; i++)
                {
                    byte* start = ptr;
                    MarshalEntities.MarshalHeroPower(heroPower, start);
                }
                watch3.Stop();
                var result3 = Marshal.PtrToStructure<MMFEntities.HeroPower>((IntPtr) ptr);

                watch4.Start();
                for (int i = 0; i < COUNT; i++)
                {
                    MarshalEntities.MarshalHeroPowerPtr(heroPower, (int*)ptr);
                }
                watch4.Stop();
                var result4 = Marshal.PtrToStructure<MMFEntities.HeroPower>((IntPtr) ptr);

                if (result3 != result4)
                    throw new Exception();
            }

            Console.WriteLine($"Marshal Minion using Marshal.StructureToPtr: {watch1.ElapsedMilliseconds} ms");
            Console.WriteLine($"Marshal Minion using raw pointer: {watch2.ElapsedMilliseconds} ms");
            Console.WriteLine($"Marshal HeroPower using Marshal.StructureToPtr: {watch3.ElapsedMilliseconds} ms");
            Console.WriteLine($"Marshal HeroPower using raw pointer: {watch4.ElapsedMilliseconds} ms");
        }
    }
}
