using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SabberEntities = SabberStoneCore.Model.Entities;

namespace SabberStone_gRPC.MMF.Entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public readonly struct Playable
    {
        public readonly int CardId;
        public readonly int Cost;
        public readonly int ATK;
        public readonly int BaseHealth;

        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Ghostly;

        public Playable(SabberEntities.IPlayable playable, bool hand)
        {
            CardId = playable.Card.AssetId;
            Cost = playable.Cost;
            if (playable is SabberEntities.Character c)
            {
                ATK = c.AttackDamage;
                BaseHealth = c.BaseHealth;
            }
            else
            {
                ATK = 0;
                BaseHealth = 0;
            }

            if (hand)
                Ghostly = playable[SabberStoneCore.Enums.GameTag.GHOSTLY] == 1;
            else
                Ghostly = false;
        }

        internal Playable(int a, int b, int c, int d, bool e)
        {
            CardId = a;
            Cost = b;
            ATK = c;
            BaseHealth = d;
            Ghostly = e;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public readonly struct HeroPower
    {
        public readonly int CardId;
        public readonly int Cost;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Exhausted;

        public HeroPower(SabberEntities.HeroPower playable)
        {
            CardId = playable.Card.AssetId;
            Cost = playable.Cost;
            Exhausted = playable.IsExhausted;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public readonly struct Weapon
    {
        public readonly int CardId;
        public readonly int ATK;
        public readonly int Durability;
        public readonly int Damage;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Windfury;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Lifesteal;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Immune;

        public Weapon(SabberEntities.Weapon playable)
        {
            CardId = playable.Card.AssetId;
            ATK = playable.AttackDamage;
            Durability = playable.Durability;
            Damage = playable.Damage;
            Windfury = playable.IsWindfury;
            Lifesteal = playable.HasLifeSteal;
            Immune = playable.IsImmune;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public readonly struct Hero
    {
        public readonly int CardClass;
        public readonly int ATK;
        public readonly int BaseHealth;
        public readonly int Damage;
        public readonly int NumAttacksThisTurn;
        public readonly int Armor;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Exhausted;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Stealth;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Immune;
        public readonly HeroPower HeroPower;
        public readonly Weapon Weapon;

        public Hero(SabberEntities.Hero playable)
        {
            CardClass = (int)playable.Card.Class;
            ATK = playable.AttackDamage;
            BaseHealth = playable.BaseHealth;
            Damage = playable.Damage;
            NumAttacksThisTurn = playable.NumAttacksThisTurn;
            Armor = playable.Armor;
            Exhausted = playable.IsExhausted;
            Stealth = playable.HasStealth;
            Immune = playable.IsImmune;
            HeroPower = new HeroPower(playable.HeroPower);
            Weapon = new Weapon(playable.Weapon);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public readonly struct Minion
    {
        public readonly int CardId;
        public readonly int ATK;
        public readonly int BaseHealth;
        public readonly int Damage;
        public readonly int NumAttacksThisTurn;
        public readonly int ZonePosition;
        public readonly int OrderOfPlay;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Exhausted;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Stealth;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Immune;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Charge;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool AttackableByRush;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Windfury;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Lifesteal;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Taunt;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool DivineShield;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Elusive;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Frozen;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Deathrattle;
        [MarshalAs(UnmanagedType.U1)]
        public readonly bool Silenced;

        public Minion(SabberEntities.Minion playable)
        {
            CardId = playable.Card.AssetId;
            ATK = playable.AttackDamage;
            BaseHealth = playable.BaseHealth;
            Damage = playable.Damage;
            NumAttacksThisTurn = playable.NumAttacksThisTurn;
            ZonePosition = playable.ZonePosition;
            OrderOfPlay = playable.OrderOfPlay;
            Exhausted = playable.IsExhausted;
            Stealth = playable.HasStealth;
            Immune = playable.IsImmune;
            Charge = playable.HasCharge;
            AttackableByRush = playable.AttackableByRush;
            Windfury = playable.HasWindfury;
            Lifesteal = playable.HasLifeSteal;
            Taunt = playable.HasTaunt;
            DivineShield = playable.HasDivineShield;
            Elusive = playable.CantBeTargetedBySpells;
            Frozen = playable.IsFrozen;
            Deathrattle = playable.HasDeathrattle;
            Silenced = playable.IsSilenced;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public readonly struct Controller
    {
        public readonly int Id;
        public readonly int PlayState;
        public readonly int BaseMana;
        public readonly int RemainingMana;
        public readonly int OverloadLocked;
        public readonly int OverloadOwed;
        public readonly Hero Hero;
        public readonly HandZone HandZone;
        public readonly BoardZone BoardZone;
        public readonly SecretZone SecretZone;
        public readonly DeckZone DeckZone;

        public Controller(SabberEntities.Controller controller)
        {
            Id = controller.PlayerId;
            PlayState = (int)controller.PlayState;
            BaseMana = controller.BaseMana;
            RemainingMana = controller.RemainingMana;
            OverloadLocked = controller.OverloadLocked;
            OverloadOwed = controller.OverloadOwed;
            Hero = new Hero(controller.Hero);
            HandZone = new HandZone(controller.HandZone);
            BoardZone = new BoardZone(controller.BoardZone);
            SecretZone = new SecretZone(controller.SecretZone);
            DeckZone = new DeckZone(controller.DeckZone);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public readonly struct Game
    {
        private static int _id_gen;

        public readonly int Id;
        public readonly int State;
        public readonly int Turn;
        public readonly Controller CurrentPlayer;
        public readonly Controller CurrentOpponent;

        public Game(SabberStoneCore.Model.Game game, int id = -1)
        {
            if (id < 0)
            {
                Id = _id_gen++;
            }
            else
                Id = id;

            State = (int)game.State;
            Turn = game.Turn;
            CurrentPlayer = new Controller(game.CurrentPlayer);
            CurrentOpponent = new Controller(game.CurrentOpponent);
        }
    }
}
