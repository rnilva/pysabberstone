using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SabberStoneCore.Model.Entities;
using SModel = SabberStoneCore.Model;
using MMFEntities = SabberStone_gRPC.MMF.Entities;
using Controller = SabberStone_gRPC.MMF.Entities.Controller;

namespace SabberStone_gRPC.MMF
{
    public static class MarshalEntities
    {
        public static unsafe int MarshalGameToMMF(SModel.Game game, in byte* mmfPtr, int id)
        {
            int* ip = (int*)mmfPtr;

            *ip++ = id;
            *ip++ = (int)game.State;
            *ip++ = game.Turn;

            ip = MarshalController(game.CurrentPlayer, ip);
            ip = MarshalController(game.CurrentOpponent, ip);

            return (int) ((byte*)ip - mmfPtr);
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
            ip = MarshalHandZone(controller.HandZone, ip);
            ip = MarshalBoardZone(controller.BoardZone, ip);
            ip = MarshalSecretZone(controller.SecretZone, ip);
            ip = MarshalDeckZone(controller.DeckZone, ip);

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

            ip = (int*)bp;
            ip = MarshalHeroPowerPtr(playable.HeroPower, ip);
            ip = MarshalWeaponPtr(playable.Weapon, ip, out bool exist);

            return ip;
        }
        public static unsafe int* MarshalHeroPower(SModel.Entities.HeroPower playable, byte* bp)
        {
            Marshal.StructureToPtr(new MMFEntities.HeroPower(playable), (IntPtr) bp, false);
            bp += Marshal.SizeOf<MMFEntities.HeroPower>();

            return (int*)bp;
        }

        public static unsafe int* MarshalHeroPowerPtr(SModel.Entities.HeroPower playable, int* ip)
        {
            *ip++ = playable.Card.AssetId;
            *ip++ = playable.Cost;
            byte* bp = (byte*) ip;
            *bp++ = Convert.ToByte(playable.IsExhausted);
            return (int*) bp;
        }

        private static unsafe byte* MarshalWeapon(SModel.Entities.Weapon playable, byte* bp)
        {
            Marshal.StructureToPtr(new MMFEntities.Weapon(playable), (IntPtr) bp, false);
            bp += Marshal.SizeOf<MMFEntities.Weapon>();

            return bp;
        }

        public static unsafe int* MarshalWeaponPtr(SModel.Entities.Weapon playable, int* ip, out bool exist)
        {
            if (playable is null)
            {
                exist = false;
                *ip++ = 0;
                return ip;
            }

            *ip++ = playable.Card.AssetId;
            *ip++ = playable.AttackDamage;
            *ip++ = playable.Durability;
            *ip++ = playable.Damage;

            byte* bp = (byte*) ip;
            *bp++ = Convert.ToByte(playable.IsWindfury);
            *bp++ = Convert.ToByte(playable.HasLifeSteal);
            *bp++ = Convert.ToByte(playable.IsImmune);

            exist = true;
            return (int*)bp;
        }

        public static unsafe byte* MarshalMinion(SModel.Entities.Minion playable, byte* bp)
        {
            Marshal.StructureToPtr(new MMFEntities.Minion(playable), (IntPtr) bp, false);
            bp += Marshal.SizeOf<MMFEntities.Minion>();

            return bp;
        }

        public static unsafe int* MarshalMinionPtr(SModel.Entities.Minion playable, int* ip)
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

            return (int*)bp;
        }

        public static unsafe int* MarshalPlayable(SModel.Entities.IPlayable playable, int* ip, bool hand)
        {
            *ip++ = playable.Card.AssetId;
            *ip++ = playable.Cost;
            if (playable is SModel.Entities.Character c)
            {
                *ip++ = c.AttackDamage;
                *ip++ = c.BaseHealth;
            }
            else if (playable is SModel.Entities.Weapon w)
            {
                *ip++ = w.AttackDamage;
                *ip++ = w.Durability;
            }
            else
            {
                *ip++ = -1;
                *ip++ = -1;
            }

            byte* bp = (byte*) ip;
            if (hand)
                *bp++ = (byte) playable[SabberStoneCore.Enums.GameTag.GHOSTLY];
            else
                *bp++ = (byte) 0;

            return (int*)bp;
        }

        public static unsafe int* MarshalHandZone(SModel.Zones.HandZone zone, int* ip)
        {
            ReadOnlySpan<IPlayable> span = zone.GetSpan();

            *ip++ = span.Length; // Count
            for (int i = 0; i < span.Length; i++) 
                ip = MarshalPlayable(span[i], ip, true);

            return ip;
        }

        public static unsafe int* MarshalBoardZone(SModel.Zones.BoardZone zone, int* ip)
        {
            ReadOnlySpan<Minion> span = zone.GetSpan();

            *ip++ = span.Length; // Count
            for (int i = 0; i < span.Length; i++) 
                ip = MarshalMinionPtr(span[i], ip);

            return ip;
        }

        public static unsafe int* MarshalSecretZone(SModel.Zones.SecretZone zone, int* ip)
        {
            ReadOnlySpan<Spell> span = zone.GetSpan();

            *ip++ = span.Length; // Count
            for (int i = 0; i < span.Length; i++) 
                ip = MarshalPlayable(span[i], ip, false);

            return ip;
        }

        public static unsafe int* MarshalDeckZone(SModel.Zones.DeckZone zone, int* ip)
        {
            ReadOnlySpan<IPlayable> span = zone.GetSpan();

            *ip++ = span.Length; // Count
            for (int i = 0; i < span.Length; i++) 
                ip = MarshalPlayable(span[i], ip, false);

            return ip;
        }
    }
}
