using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SModel = SabberStoneCore.Model;
using MMFEntities = SabberStone_gRPC.MMF.Entities;
using Controller = SabberStone_gRPC.MMF.Entities.Controller;

namespace SabberStone_gRPC.MMF
{
    public static class MarshalEntities
    {
        public static unsafe void MarshalGameToMMF(SModel.Game game, MemoryMappedFile mmf, int id)
        {
            using (MemoryMappedViewAccessor view = mmf.CreateViewAccessor())
            {
                byte* ptr = null;
                view.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);

                int* ip = (int*)ptr;
                *ip++ = id;
                *ip++ = (int)game.State;
                *ip++ = game.Turn;

                int* offset = MarshalController(game.CurrentPlayer, ip);
            }
        }

        private static unsafe int* MarshalController(SModel.Entities.Controller controller, int* ip)
        {
            *ip++ = controller.PlayerId;
            *ip++ = (int)controller.PlayState;
            *ip++ = controller.BaseMana;
            *ip++ = controller.RemainingMana;
            *ip++ = controller.OverloadLocked;
            *ip++ = controller.OverloadOwed;


            ip = MarshalHero(controller.Hero, ip);
            

            return ip;
        }

        private static unsafe int* MarshalHero(SModel.Entities.Hero playable, int* ip)
        {
            *ip++ = (int)playable.Card.Class;
            *ip++ = playable.AttackDamage;
            *ip++ = playable.BaseHealth;
            *ip++ = playable.Damage;
            *ip++ = playable.NumAttacksThisTurn;
            *ip++ = playable.Armor;

            byte* bp = (byte*) ip;
            *bp++ = Convert.ToByte(playable.IsExhausted);
            *bp++ = Convert.ToByte(playable.HasStealth);
            *bp++ = Convert.ToByte(playable.IsImmune);


            bp = MarshalHeroPower(playable.HeroPower, bp);
            bp = MarshalWeapon(playable.Weapon, bp);

            return (int*)bp;
        }
        public static unsafe byte* MarshalHeroPower(SModel.Entities.HeroPower playable, byte* bp)
        {
            Marshal.StructureToPtr(new MMFEntities.HeroPower(playable), (IntPtr) bp, false);
            bp += Marshal.SizeOf<MMFEntities.HeroPower>();

            return bp;
        }

        public static unsafe byte* MarshalHeroPowerPtr(SModel.Entities.HeroPower playable, int* ip)
        {
            *ip++ = playable.Card.AssetId;
            *ip++ = playable.Cost;
            byte* bp = (byte*) ip;
            *bp++ = Convert.ToByte(playable.IsExhausted);
            return bp;
        }

        private static unsafe byte* MarshalWeapon(SModel.Entities.Weapon playable, byte* bp)
        {
            Marshal.StructureToPtr(new MMFEntities.Weapon(playable), (IntPtr) bp, false);
            bp += Marshal.SizeOf<MMFEntities.Weapon>();

            return bp;
        }

        public static unsafe byte* MarshalMinion(SModel.Entities.Minion playable, byte* bp)
        {
            Marshal.StructureToPtr(new MMFEntities.Minion(playable), (IntPtr) bp, false);
            bp += Marshal.SizeOf<MMFEntities.Minion>();

            return bp;
        }

        public static unsafe byte* MarshalMinionPtr(SModel.Entities.Minion playable, int* ip)
        {
            *ip++ = playable.Card.AssetId;
            *ip++ = playable.AttackDamage;
            *ip++ = playable.BaseHealth;
            *ip++ = playable.Damage;
            *ip++ = playable.NumAttacksThisTurn;
            *ip++ = playable.ZonePosition;
            *ip++ = playable.OrderOfPlay;

            byte* bp = (byte*)ip;
            *bp++ = Convert.ToByte(playable.IsExhausted);
            *bp++ = Convert.ToByte(playable.HasStealth);
            *bp++ = Convert.ToByte(playable.IsImmune);
            *bp++ = Convert.ToByte(playable.HasCharge);
            *bp++ = Convert.ToByte(playable.AttackableByRush);
            *bp++ = Convert.ToByte(playable.HasWindfury);
            *bp++ = Convert.ToByte(playable.HasLifeSteal);
            *bp++ = Convert.ToByte(playable.HasTaunt);
            *bp++ = Convert.ToByte(playable.HasDivineShield);
            *bp++ = Convert.ToByte(playable.CantBeTargetedBySpells);
            *bp++ = Convert.ToByte(playable.IsFrozen);
            *bp++ = Convert.ToByte(playable.HasDeathrattle);
            *bp++ = Convert.ToByte(playable.IsSilenced);

            return bp;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void WriteUInt(ref uint* ptr, uint value)
        {
            *ptr++ = value;
        }
    }
}
