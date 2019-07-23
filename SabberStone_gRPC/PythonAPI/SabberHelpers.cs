using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SabberStoneCore.Enums;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStonePython.API;
using Card = SabberStoneCore.Model.Card;
using Cards = SabberStoneCore.Model.Cards;
using Game = SabberStoneCore.Model.Game;
using Controller = SabberStoneCore.Model.Entities.Controller;
using Minion = SabberStoneCore.Model.Entities.Minion;

namespace SabberStonePython
{
    public static class SabberHelpers
    {
        public static API.Game GenerateGameAPI(string deckString1, string deckString2)
        {
            Deck deck1, deck2;

            try
            {
                deck1 = Deserialise(deckString1);
            } 
            catch (Exception e)
            {
                Console.WriteLine("Deckstring #1 is not a valid deckstring");
                throw e;
            }

            try
            {
                deck2 = Deserialise(deckString2);
            }
            catch (Exception e)
            {
                Console.WriteLine("Deckstring #2 is not a valid deckstring");
                throw e;
            }



            var game = new Game(new SabberStoneCore.Config.GameConfig
            {
                StartPlayer = -1,
                Player1HeroClass = deck1.Class,
                Player1Deck = deck1.GetCardList(),
                Player2HeroClass = deck2.Class,
                Player2Deck = deck2.GetCardList(),

                Logging = false,
                History = false,
                FillDecks = false,
                Shuffle = true,
                SkipMulligan = true,
            });
            game.StartGame();

            Console.WriteLine(Printers.PrintGame(game));

            return new API.Game(game);
        }

        public static PlayerTask GetPlayerTask(API.Option option, Game g)
        {
            const bool SkipPrePhase = true;
            EntityList dict;
            Controller c = g.CurrentPlayer;

            switch (option.Type)
            {
                case Option.Types.PlayerTaskType.Choose:
                    return ChooseTask.Pick(c, option.Choice);
                case Option.Types.PlayerTaskType.Concede:
                    return ConcedeTask.Any(c);
                case Option.Types.PlayerTaskType.EndTurn:
                    return EndTurnTask.Any(c);
                case Option.Types.PlayerTaskType.HeroAttack:
                    return HeroAttackTask.Any(c, (ICharacter)g.IdEntityDic[option.TargetId], SkipPrePhase);
                case Option.Types.PlayerTaskType.HeroPower:
                    return HeroPowerTask.Any(c,
                        option.TargetId > 0 ? (ICharacter) g.IdEntityDic[option.TargetId] : null, option.SubOption, SkipPrePhase);
                case Option.Types.PlayerTaskType.MinionAttack:
                    dict = g.IdEntityDic;
                    return MinionAttackTask.Any(c, dict[option.SourceId], (ICharacter) dict[option.TargetId],
                        SkipPrePhase);
                case Option.Types.PlayerTaskType.PlayCard:
                    dict = g.IdEntityDic;
                    return PlayCardTask.Any(c, dict[option.SourceId],
                        option.TargetId > 0 ? (ICharacter) dict[option.TargetId] : null,
                        option.ZonePosition, option.SubOption, SkipPrePhase);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static class Printers
        {
            public static string PrintAction(PlayerTask a)
            {
                var name = a.Controller.Name;

                switch (a.PlayerTaskType)
                {
                    case PlayerTaskType.CHOOSE:
                        var c = a as ChooseTask;
                        return $"[{name}] chose {c.Game.IdEntityDic[c.Choices[0]]}";
                    case PlayerTaskType.END_TURN:
                        return $"[{name}] ended his turn.";
                    case PlayerTaskType.HERO_ATTACK:
                        return $"[{name}]'s {a.Controller.Hero} attacked {a.Target}";
                    case PlayerTaskType.HERO_POWER:
                        return $"[{name}] used {a.Controller.Hero.HeroPower}" + $"{(a.Target != null ? $" to {a.Target}" : "")}" + $"{(a.Controller.Hero.HeroPower.ChooseOne ? $" with SubOption: {a.ChooseOne}" : "")}";
                    case PlayerTaskType.MINION_ATTACK:
                        return $"[{name}]'s {a.Source} attacked {a.Target}";
                    case PlayerTaskType.PLAY_CARD:
                        if (a.Source is Minion)
                            return $"[{name}] played {a.Source} to Position {((PlayCardTask)a).ZonePosition}" + $"{(a.Target != null ? $" and targeted {a.Target}" : "")}" + $" {(a.Source.Card.ChooseOne ? $" with SubOption: {a.ChooseOne}" : "")}";
                        else
                            return $"[{name}] played {a.Source}" + $"{(a.Target != null ? $" to {a.Target}" : "")}" + $"{(a.Source.Card.ChooseOne ? $" with SubOption: {a.ChooseOne}" : "")}";
                    default:
                        return "";
                }
            }

            public static string PrintGame(Game game)
            {
                var current = game.CurrentPlayer;
                var opponent = game.CurrentOpponent;
                var sb = new StringBuilder();
                sb.AppendLine("");
                sb.AppendLine($"ROUND {(game.Turn + 1) / 2} - {current.Name}\n");
                //sb.AppendLine($"Hero[P1]: {game.Player1.Hero.Health + game.Player1.Hero.Armor} / Hero[P2]: {game.Player2.Hero.Health + game.Player2.Hero.Armor}");
                sb.AppendLine($"[Op Hero: {opponent.Hero.Health}{(opponent.Hero.Armor == 0 ? "" : $" + {opponent.Hero.Armor}")}][{game.CurrentOpponent.Hero}]");
                if (opponent.Hero.Weapon != null)
                {
                    sb.AppendLine($"[Op Weapon: {opponent.Hero.Weapon}({opponent.Hero.Weapon.AttackDamage}/{opponent.Hero.Weapon.Durability})]");
                }

                if (opponent.SecretZone.Count > 0 || opponent.SecretZone.Quest != null)
                {
                    sb.Append($"[Op Secrets: ");
                    opponent.SecretZone.ToList().ForEach(p =>
                    {
                        var s = p;
                        if (s.IsQuest)
                            sb.Append($"{s}({s.QuestProgress})");
                        else
                            sb.Append(s);
                    });
                    sb.Append("]\n");
                }

                //if (opponent.SecretZone.Quest != null)
                //{
                // sb.Append("[Op Quest: ");

                //}
                sb.Append("[Op Board: ");
                opponent.BoardZone.ToList().ForEach(m =>
                {
                    var str = new StringBuilder();
                    str.Append($"({m.AttackDamage}/{m.Health})");
                    str.Append(m);
                    str.Append(" | ");
                    sb.Append(str.ToString());
                });
                sb.Append("]\n");
                sb.Append("[Board: ");
                current.BoardZone.ToList().ForEach(m =>
                {
                    var str = new StringBuilder();
                    str.Append($"({m.AttackDamage}/{m.Health})");
                    str.Append(m);
                    str.Append(" | ");
                    sb.Append(str.ToString());
                });
                sb.Append("]\n");
                sb.AppendLine($"[Hero: {current.Hero.Health}{(current.Hero.Armor == 0 ? "" : $" + {current.Hero.Armor}")}][{game.CurrentPlayer.Hero}][Power: ({current.Hero.HeroPower.Cost}){current.Hero.HeroPower}]");
                if (current.Hero.Weapon != null)
                {
                    sb.AppendLine($"[Weapon: {current.Hero.Weapon}({current.Hero.Weapon.AttackDamage}/{current.Hero.Weapon.Durability})]");
                }
                if (current.SecretZone.Count > 0 || current.SecretZone.Quest != null)
                {
                    sb.Append($"[Secrets: ");
                    current.SecretZone.ToList().ForEach(s =>
                    {
                        if (s.IsQuest)
                            sb.Append($"{s}({s.QuestProgress})");
                        else
                            sb.Append(s);
                    });
                    sb.Append("]\n");
                }
                sb.Append("[Hand: ");
                current.HandZone.ToList().ForEach(p =>
                {
                    sb.Append($"({p.Cost})");
                    sb.Append(p);
                });
                sb.Append("]\n");
                sb.AppendLine($"[Mana:{current.RemainingMana}/{current.OverloadOwed}/{current.BaseMana}][{current.OverloadLocked}]");

                return sb.ToString();
            }
        }

        public class Deck
        {
            private readonly IReadOnlyDictionary<int, int> _cards;

            public CardClass Class { get; }
            public FormatType Format { get; }
            public string Name { get; }

            public Deck(int heroId, IReadOnlyDictionary<int, int> idsAndCounts, FormatType format, string name)
            {
                Name = name;
                Format = format;
                Class = Cards.FromAssetId(heroId).Class;
                _cards = idsAndCounts;
            }

            public List<Card> GetCardList()
            {
                var result = new List<Card>(30);
                foreach (KeyValuePair<int, int> item in _cards)
                {
                    Card card = Cards.FromAssetId(item.Key);
                    for (int i = 0; i < item.Value; i++)
                        result.Add(card);
                }
                return result;
            }
        }

        public static Deck Deserialise(string deckString, string deckName = null)
        {
            string[] lines = deckString.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrEmpty(line)) continue;

                if (line.StartsWith('#'))
                {
                    if (deckName == null && line.StartsWith("###"))
                        deckName = line.Substring(3).Trim();
                    continue;
                }

                byte[] bytes = Convert.FromBase64String(line);
                // Leading 0
                int offset = 1;
                int length;

                // Version
                ReadVarint(bytes, ref offset, out length);

                FormatType format = (FormatType)ReadVarint(bytes, ref offset, out length);

                ReadVarint(bytes, ref offset, out length);

                int heroId = ReadVarint(bytes, ref offset, out length);

                Dictionary<int, int> cardIdsAndCountPairs = new Dictionary<int, int>();
                for (int j = ReadVarint(bytes, ref offset, out length); j > 0; --j)
                    cardIdsAndCountPairs.Add(ReadVarint(bytes, ref offset, out length), 1);
                for (int j = ReadVarint(bytes, ref offset, out length); j > 0; --j)
                    cardIdsAndCountPairs.Add(ReadVarint(bytes, ref offset, out length), 2);
                for (int j = ReadVarint(bytes, ref offset, out length); j > 0; --j)
                {
                    int id = ReadVarint(bytes, ref offset, out length);
                    int count = ReadVarint(bytes, ref offset, out length);
                    cardIdsAndCountPairs.Add(id, count);
                }

                return new Deck(heroId, cardIdsAndCountPairs, format, deckName);
            }

            throw new ArgumentException();
        }

        private static int ReadVarint(byte[] bytes, ref int offset, out int length)
        {
            int result = length = 0;
            for (int i = offset; i < bytes.Length; i++)
            {
                int value = (int) bytes[i] & 0x7f;
                result |= value << length * 7;
                if ((bytes[i] & 0x80) != 0x80)
                    break;
                length++;
            }

            length++;

            offset += length;
            return result;
        }
    }
}
