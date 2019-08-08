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
            const int BaseSize = 4 * 3;

            int* ip = (int*)mmfPtr;

            *ip++ = id;
            *ip++ = (int)game.State;
            *ip++ = game.Turn;

            ip = MarshalController(game.CurrentPlayer, ip, out int cpSize);
            MarshalController(game.CurrentOpponent, ip, out int opSize);

            return BaseSize + cpSize + opSize;
            
        }

        private static unsafe int* MarshalController(SModel.Entities.Controller controller, int* ip, out int size)
        {
            const int BaseSize = 4 * 10;

            *ip++ = controller.PlayerId;
            *ip++ = (int)controller.PlayState;
            *ip++ = controller.BaseMana;
            *ip++ = controller.RemainingMana;
            *ip++ = controller.OverloadLocked;
            *ip++ = controller.OverloadOwed;

            ip = MarshalHero(controller.Hero, ip, out int heroSize);
            ip = MarshalHandZone(controller.HandZone, ip, out int handCount);
            ip = MarshalBoardZone(controller.BoardZone, ip, out int boardCount);
            ip = MarshalSecretZone(controller.SecretZone, ip, out int secretCount);
            ip = MarshalDeckZone(controller.DeckZone, ip, out int deckCount);

            int pSize = MMFEntities.Playable.Size;
            int mSize = MMFEntities.Minion.Size;
            size = BaseSize + heroSize + (handCount + secretCount + deckCount) * pSize + boardCount * mSize;
            return ip;
        }

        private static unsafe int* MarshalHero(SModel.Entities.Hero playable, int* ip, out int size)
        {
            const int IntCount = 6;
            const int ByteCount = 3;
            const int HeroPowerSize = 9;
            const int WeaponSize = 19;

            const int BaseSize = 4 * IntCount + 1 * ByteCount + HeroPowerSize;

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

            size = BaseSize + (exist ? WeaponSize : 4);
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
                return ++ip;
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
            else
                ip += 2;

            byte* bp = (byte*) ip;
            if (hand)
                *bp++ = (byte) playable[SabberStoneCore.Enums.GameTag.GHOSTLY];
            else
                bp += 1;

            return (int*)bp;
        }

        public static unsafe int* MarshalHandZone(SModel.Zones.HandZone zone, int* ip, out int count)
        {
            ReadOnlySpan<IPlayable> span = zone.GetSpan();

            *ip++ = span.Length; // Count
            for (int i = 0; i < span.Length; i++) 
                ip = MarshalPlayable(span[i], ip, true);

            count = span.Length;
            return ip;
        }

        public static unsafe int* MarshalBoardZone(SModel.Zones.BoardZone zone, int* ip, out int count)
        {
            ReadOnlySpan<Minion> span = zone.GetSpan();

            *ip++ = span.Length; // Count
            for (int i = 0; i < span.Length; i++) 
                ip = MarshalMinionPtr(span[i], ip);

            count = span.Length;
            return ip;
        }

        public static unsafe int* MarshalSecretZone(SModel.Zones.SecretZone zone, int* ip, out int count)
        {
            ReadOnlySpan<Spell> span = zone.GetSpan();

            *ip++ = span.Length; // Count
            for (int i = 0; i < span.Length; i++) 
                ip = MarshalPlayable(span[i], ip, false);

            count = span.Length;
            return ip;
        }

        public static unsafe int* MarshalDeckZone(SModel.Zones.DeckZone zone, int* ip, out int count)
        {
            ReadOnlySpan<IPlayable> span = zone.GetSpan();

            *ip++ = span.Length; // Count
            for (int i = 0; i < span.Length; i++) 
                ip = MarshalPlayable(span[i], ip, false);

            count = span.Length;
            return ip;
        }
    }
}
