using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;

namespace SabberStonePython.Tests
{
    public class UtilTests
    {
        public static void DeckStringDeserialize()
        {
            string deckString = @"AAEBAf0EAA8MTXHDAZ4CuwKLA5UDvwOABLQE5gSgBewFuQYA";
            List<Card> mageExpert = new List<Card>
            {
                Cards.FromName("Mana Addict"),
                Cards.FromName("Mana Addict"),
                Cards.FromName("Polymorph"),
                Cards.FromName("Polymorph"),
                Cards.FromName("Counterspell"),
                Cards.FromName("Counterspell"),
                Cards.FromName("Mirror Entity"),
                Cards.FromName("Mirror Entity"),
                Cards.FromName("Vaporize"),
                Cards.FromName("Vaporize"),
                Cards.FromName("Fireball"),
                Cards.FromName("Fireball"),
                Cards.FromName("Water Elemental"),
                Cards.FromName("Water Elemental"),
                Cards.FromName("Mana Wyrm"),
                Cards.FromName("Mana Wyrm"),
                Cards.FromName("Arcane Explosion"),
                Cards.FromName("Arcane Explosion"),
                Cards.FromName("Frost Elemental"),
                Cards.FromName("Frost Elemental"),
                Cards.FromName("Arcane Missiles"),
                Cards.FromName("Arcane Missiles"),
                Cards.FromName("Sorcerer's Apprentice"),
                Cards.FromName("Sorcerer's Apprentice"),
                Cards.FromName("Kobold Geomancer"),
                Cards.FromName("Kobold Geomancer"),
                Cards.FromName("Kirin Tor Mage"),
                Cards.FromName("Kirin Tor Mage"),
                Cards.FromName("Azure Drake"),
                Cards.FromName("Azure Drake"),
            };

            SabberHelpers.Deck deck = SabberHelpers.Deserialise(deckString, "Mage Expert");

            List<Card> list = deck.GetCardList();
            CardClass cls = deck.Class;

            if (cls != CardClass.MAGE || !Enumerable.SequenceEqual(mageExpert, list))
                throw new Exception();
        }
    }
}
