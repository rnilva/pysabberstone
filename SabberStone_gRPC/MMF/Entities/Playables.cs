using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SabberEntities = SabberStoneCore.Model.Entities;

namespace SabberStone_gRPC.MMF.Entities
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public readonly struct Playable
    {
        public readonly int CardId;
        public readonly int Cost;
        public readonly int ATK;
        public readonly int BaseHealth;
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
    }
}
