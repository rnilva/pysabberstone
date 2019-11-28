using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SabberStoneContract.Core;
using SabberStoneContract.Model;
using SabberStoneCore.Enums;
using SabberStoneCore.Kettle;
using SabberStoneCore.Loader;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStonePython.API;
using Card = SabberStoneCore.Model.Card;
using Cards = SabberStoneCore.Model.Cards;
using Game = SabberStoneCore.Model.Game;
using Controller = SabberStoneCore.Model.Entities.Controller;
using Minion = SabberStoneCore.Model.Entities.Minion;
using Spell = SabberStoneCore.Model.Entities.Spell;
using Hero = SabberStoneCore.Model.Entities.Hero;
using HeroPower = SabberStoneCore.Model.Entities.HeroPower;
using Weapon = SabberStoneCore.Model.Entities.Weapon;
using GameTag = SabberStoneCore.Enums.GameTag;
using static SabberStonePython.API.Option.Types.PlayerTaskType;

namespace SabberStonePython
{
    public static class SabberHelpers
    {
        public static Game GenerateGame(string deckString1, string deckString2, bool history = false,
                                        long seed = 0)
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

            if (seed == 0) seed = DateTime.UtcNow.Ticks;

            var game = new Game(new SabberStoneCore.Config.GameConfig
            {
                StartPlayer = -1,
                Player1HeroClass = deck1.Class,
                Player1Deck = deck1.GetCardList(),
                Player2HeroClass = deck2.Class,
                Player2Deck = deck2.GetCardList(),

                Logging = false,
                History = history,
                FillDecks = false,
                Shuffle = true,
                SkipMulligan = true,
                RandomSeed = seed
            });
            game.StartGame();

            return game;
        }

        public static API.Game GenerateGameAPI(string deckString1, string deckString2)
        {
            var game = GenerateGame(deckString1, deckString2);

            Console.WriteLine(Printers.PrintGame(game));

            return new API.Game(game);
        }

        public static List<Option> PythonOptions(this Controller c, int gameId)
        {
            if (c.Choice != null)
            {
                if (c.Choice.ChoiceType == ChoiceType.GENERAL)
                    return c.Choice.Choices.Select(p => new Option(gameId, p, c.Game.IdEntityDic[p].Card.Name)).ToList();

                throw new NotImplementedException();
            }

            int controllerId = c.Id;
            List<Option> allOptions = ManagedObjects.OptionBuffers[gameId];

            allOptions.Add(new Option(gameId, EndTurn));

            #region PlayCardTasks
			int mana = c.RemainingMana;
			int zonePosRange = c.BoardZone.Count;
			bool? spellCostHealth = null;

            Character[] allTargets = null;
            Minion[] friendlyMinions = null;
            Minion[] enemyMinions = null;
            Minion[] allMinions = null;
            Character[] allFriendly = null;
            Character[] allEnemies = null;

            ReadOnlySpan<IPlayable> handSpan = c.HandZone.GetSpan();
			for (int i = 0; i < handSpan.Length; i++)
			{
				if (!handSpan[i].ChooseOne || c.ChooseBoth)
					GetPlayCardTasks(handSpan[i]);
				else
				{
					IPlayable[] playables = handSpan[i].ChooseOnePlayables;
					for (int j = 1; j < 3; j++)
						GetPlayCardTasks(handSpan[i], playables[j - 1], j);
				}
			}
			#endregion

			#region HeroPowerTask
			HeroPower power = c.Hero.HeroPower;
			Card heroPowerCard = power.Card;
			if (!power.IsExhausted && mana >= power.Cost &&
			    !c.HeroPowerDisabled && !heroPowerCard.HideStat)
			{
				if (heroPowerCard.ChooseOne)
				{
                    if (c.ChooseBoth)
                        allOptions.Add(new Option(gameId, API.Option.Types.PlayerTaskType.HeroPower, source: power));
                    else
                    {
                        allOptions.Add(new Option(gameId, API.Option.Types.PlayerTaskType.HeroPower, subOption: 1, source: power));
                        allOptions.Add(new Option(gameId, API.Option.Types.PlayerTaskType.HeroPower, subOption: 2, source: power));
                    }
                }
				else
				{
					if (heroPowerCard.IsPlayableByCardReq(c))
					{
						Character[] targets = GetTargets(heroPowerCard);
						if (targets != null)
							for (int i = 0; i < targets.Length; i++)
                                allOptions.Add(new Option(gameId, API.Option.Types.PlayerTaskType.HeroPower, 
                                    0, Option.getPosition(targets[i], controllerId),
                                    source: power, target: targets[i]));
						else
                            allOptions.Add(new Option(gameId, API.Option.Types.PlayerTaskType.HeroPower, source: power));
                    }
				}
			}
			#endregion

			#region MinionAttackTasks
			Minion[] attackTargets = null;
			bool isOpHeroValidAttackTarget = false;
			var boardSpan = c.BoardZone.GetSpan();
			for (int j = 0; j < boardSpan.Length; j++)
			{
				Minion minion = boardSpan[j];

				if (minion.IsExhausted && (!minion.HasCharge || minion.NumAttacksThisTurn != 0))
					continue;
				if (minion.IsFrozen || minion.AttackDamage == 0 || minion.CantAttack || minion.Untouchable)
					continue;

				GenerateAttackTargets();

                for (int i = 0; i < attackTargets.Length; i++)
                    allOptions.Add(new Option(gameId, MinionAttack, j + 1, 
                        Option.getEnemyPosition(attackTargets[i]), source: minion, target: attackTargets[i]));

                if (isOpHeroValidAttackTarget && !(minion.CantAttackHeroes || minion.AttackableByRush))
                    allOptions.Add(new Option(gameId, MinionAttack, j + 1, Option.OP_HERO_POSITION,
                        source: minion, target: c.Opponent.Hero));
            }
			#endregion

			#region HeroAttackTaskts
			Hero hero = c.Hero;

			if ((!hero.IsExhausted || (hero.ExtraAttacksThisTurn > 0 && hero.ExtraAttacksThisTurn >= hero.NumAttacksThisTurn))
			    && hero.AttackDamage > 0 && !hero.IsFrozen)
			{
				GenerateAttackTargets();

                for (int i = 0; i < attackTargets.Length; i++)
                    allOptions.Add(new Option(gameId, HeroAttack, 0, 
                        Option.getEnemyPosition(attackTargets[i]), source: hero, target: attackTargets[i]));

                if (isOpHeroValidAttackTarget && !hero.CantAttackHeroes)
                    allOptions.Add(new Option(gameId, HeroAttack, 0, Option.OP_HERO_POSITION,
                        source: hero, target: c.Opponent.Hero));
            }
			#endregion

			return allOptions;

			#region local functions
			void GetPlayCardTasks(in IPlayable playable, in IPlayable chooseOnePlayable = null, int subOption = -1)
			{
				Card card = chooseOnePlayable?.Card ?? playable.Card;

				if (!spellCostHealth.HasValue)
					spellCostHealth = c.ControllerAuraEffects[GameTag.SPELLS_COST_HEALTH] == 1;

				bool healthCost = (playable.AuraEffects?.CardCostHealth ?? false) ||
				                  (spellCostHealth.Value && playable.Card.Type == CardType.SPELL);

				if (!healthCost && (playable.Cost > mana || playable.Card.HideStat))
					return;

				// check PlayableByPlayer
				switch (playable.Card.Type)
				{
					//	REQ_MINION_CAP
					case CardType.MINION when c.BoardZone.IsFull:
						return;
					case CardType.SPELL:
					{
						if (card.IsSecret)
						{
							if (c.SecretZone.IsFull) // REQ_SECRET_CAP
								return;
							if (c.SecretZone.Any(p => p.Card.AssetId == card.AssetId)) // REQ_UNIQUE_SECRET
								return;
						}

						if (card.IsQuest && c.SecretZone.Quest != null)
							return;
						break;
					}
				}

				{
					if (!card.IsPlayableByCardReq(c))
						return;

					Character[] targets = GetTargets(card);

                    int sourcePosition = playable.ZonePosition;

                    // Card doesn't require any targets
					if (targets == null)
					{
                        if (playable is Minion)
                            for (int i = 0; i <= zonePosRange; i++)
                                allOptions.Add(new Option(gameId, PlayCard, sourcePosition, i + 1, subOption,
                                    source: playable));
                        else
                            allOptions.Add(new Option(gameId, PlayCard, sourcePosition, -1, subOption,
                                source: playable));
                    }
					else
					{
						if (targets.Length == 0)
						{
							if (card.MustHaveTargetToPlay)
								return;

							if (playable is Minion)
                                for (int i = 0; i <= zonePosRange; i++)
                                    allOptions.Add(new Option(gameId, PlayCard, sourcePosition, i + 1, subOption,
                                        source: playable));
							else
								allOptions.Add(new Option(gameId, PlayCard, sourcePosition, -1, subOption,
                                    source: playable));
						}
						else
						{
                            for (int j = 0; j < targets.Length; j++)
                            {
                                ICharacter target = targets[j];
                                if (playable is Minion)
                                    //for (int i = 0; i <= zonePosRange; i++)
                                    //    allOptions.Add(PlayCardTask.Any(c, playable, target, i, subOption,
                                    //        true));
                                    continue;
                                else
                                    allOptions.Add(new Option(gameId, PlayCard, sourcePosition,
                                        Option.getPosition(target, controllerId), subOption,
                                        source: playable, target: target));
                            }
                        }
                    }
				}
			}

			// Returns null if targeting is not required
			// Returns 0 Array if there is no available target
			Character[] GetTargets(Card card)
			{
				// Check it needs additional validation
				if (!card.TargetingAvailabilityPredicate?.Invoke(c, card) ?? false)
					return null;

				Character[] targets;

				switch (card.TargetingType)
				{
					case TargetingType.None:
						return null;
					case TargetingType.All:
						if (allTargets == null)
						{
							if (c.Opponent.Hero.HasStealth)
							{
								allTargets = new Character[GetFriendlyMinions().Length + GetEnemyMinions().Length + 1];
								allTargets[0] = c.Hero;
								Array.Copy(GetAllMinions(), 0, allTargets, 1, allMinions.Length);
							}
							else
							{
								allTargets = new Character[GetFriendlyMinions().Length + GetEnemyMinions().Length + 2];
								allTargets[0] = c.Hero;
								allTargets[1] = c.Opponent.Hero;
								Array.Copy(GetAllMinions(), 0, allTargets, 2, allMinions.Length);
							}
						}
						targets = allTargets;
						break;
					case TargetingType.FriendlyCharacters:
						if (allFriendly == null)
						{
							allFriendly = new Character[GetFriendlyMinions().Length + 1];
							allFriendly[0] = c.Hero;
							Array.Copy(friendlyMinions, 0, allFriendly, 1, friendlyMinions.Length);
						}
						targets = allFriendly;
						break;
					case TargetingType.EnemyCharacters:
						if (allEnemies == null)
						{
							if (!c.Opponent.Hero.HasStealth)
							{
								allEnemies = new Character[GetEnemyMinions().Length + 1];
								allEnemies[0] = c.Opponent.Hero;
								Array.Copy(enemyMinions, 0, allEnemies, 1, enemyMinions.Length);
							}
							else
								allEnemies = GetEnemyMinions();
						}
						targets = allEnemies;
						break;
					case TargetingType.AllMinions:
						targets = GetAllMinions();
						break;
					case TargetingType.FriendlyMinions:
						targets = GetFriendlyMinions();
						break;
					case TargetingType.EnemyMinions:
						targets = GetEnemyMinions();
						break;
					case TargetingType.Heroes:
						targets = !c.Opponent.Hero.HasStealth
							? new[] { c.Hero, c.Opponent.Hero }
							: new[] { c.Hero };
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				// Filtering for target_if_available
				TargetingPredicate p = card.TargetingPredicate;
				if (p != null)
				{
					if (card.Type == CardType.SPELL || card.Type == CardType.HERO_POWER)
					{
						Character[] buffer = new Character[targets.Length];
						int i = 0;
						for (int j = 0; j < targets.Length; ++j)
						{
							if (!p(targets[j]) || targets[j].CantBeTargetedBySpells) continue;
							buffer[i] = targets[j];
							i++;
						}

						if (i != targets.Length)
						{
							Character[] result = new Character[i];
							Array.Copy(buffer, result, i);
							return result;
						}
						return buffer;
					}
					else
					{
						if (!card.TargetingAvailabilityPredicate?.Invoke(c, card) ?? false)
							return null;

						Character[] buffer = new Character[targets.Length];
						int i = 0;
						for (int j = 0; j < targets.Length; ++j)
						{
							if (!p(targets[j])) continue;
							buffer[i] = targets[j];
							i++;
						}

						if (i != targets.Length)
						{
							Character[] result = new Character[i];
							Array.Copy(buffer, result, i);
							return result;
						}
						return buffer;
					}
				}
				else if (card.Type == CardType.SPELL || card.Type == CardType.HERO_POWER)
				{
					Character[] buffer = new Character[targets.Length];
					int i = 0;
					for (int j = 0; j < targets.Length; ++j)
					{
						if (targets[j].CantBeTargetedBySpells) continue;
						buffer[i] = targets[j];
						i++;
					}

					if (i != targets.Length)
					{
						Character[] result = new Character[i];
						Array.Copy(buffer, result, i);
						return result;
					}
					return buffer;
				}

				return targets;

				Minion[] GetFriendlyMinions()
				{
					return friendlyMinions ?? (friendlyMinions = c.BoardZone.GetAll());
				}

				Minion[] GetAllMinions()
				{
					if (allMinions != null)
						return allMinions;

					allMinions = new Minion[GetEnemyMinions().Length + GetFriendlyMinions().Length];
					Array.Copy(enemyMinions, allMinions, enemyMinions.Length);
					Array.Copy(friendlyMinions, 0, allMinions, enemyMinions.Length, friendlyMinions.Length);

					return allMinions;
				}
			}

			void GenerateAttackTargets()
			{
				if (attackTargets != null) return;

				Minion[] eMinions = GetEnemyMinions();
				//var taunts = new Minion[eMinions.Length];
				Minion[] taunts = null;
				int tCount = 0;
				for (int i = 0; i < eMinions.Length; i++)
					if (eMinions[i].HasTaunt)
					{
						if (taunts == null)
							taunts = new Minion[eMinions.Length];
						taunts[tCount] = eMinions[i];
						tCount++;
					}

				if (tCount > 0)
				{
					var targets = new Minion[tCount];
					Array.Copy(taunts, targets, tCount);
					attackTargets = targets;
					isOpHeroValidAttackTarget = false;  // some brawls allow taunt heros and c should be fixed
					return;
				}
				attackTargets = eMinions;

				isOpHeroValidAttackTarget =
					!c.Opponent.Hero.IsImmune && !c.Opponent.Hero.HasStealth;
			}

			Minion[] GetEnemyMinions()
			{
				return enemyMinions ?? (enemyMinions = c.Opponent.BoardZone.GetAll(p => !p.HasStealth && !p.IsImmune));
			}
			#endregion
        }

        public static PlayerTask GetPlayerTask(Option option, Game g)
        {
            const bool SkipPrePhase = true;
            Controller c = g.CurrentPlayer;

            switch (option.Type)
            {
                case Choose:
                    return ChooseTask.Pick(c, option.Choice);
                case Concede:
                    return ConcedeTask.Any(c);
                case EndTurn:
                    return EndTurnTask.Any(c);
                case HeroAttack:
                    return HeroAttackTask.Any(c, GetOpponentTarget(option.TargetPosition), SkipPrePhase);
                case Option.Types.PlayerTaskType.HeroPower:
                    return HeroPowerTask.Any(c, GetTarget(option.TargetPosition), option.SubOption, SkipPrePhase);
                case MinionAttack:
                    return MinionAttackTask.Any(c, c.BoardZone[option.SourcePosition - 1], GetOpponentTarget(option.TargetPosition),SkipPrePhase);
                case PlayCard:
                    IPlayable source = c.HandZone[option.SourcePosition];
                    if (source.Card.Type == CardType.MINION)
                        return PlayCardTask.Any(c, source, null, option.TargetPosition - 1, option.SubOption, SkipPrePhase);
                    else
                        return PlayCardTask.Any(c, source, GetTarget(option.TargetPosition),
                            0, option.SubOption, SkipPrePhase);
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ICharacter GetOpponentTarget(int position)
            {
                if (position == Option.OP_HERO_POSITION) return c.Opponent.Hero;
                return c.Opponent.BoardZone[position - 9];
            }

            ICharacter GetTarget(int position)
            {
                if (position == -1)
                    return null;
                if (position >= Option.OP_HERO_POSITION)
                    return GetOpponentTarget(position);
                if (position == Option.HERO_POSITION)
                    return c.Hero;
                return c.BoardZone[position - 1];
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
                    opponent.SecretZone.ForEach(p =>
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
                opponent.BoardZone.ForEach(m =>
                {
                    var str = new StringBuilder();
                    str.Append($"({m.AttackDamage}/{m.Health})");
                    str.Append(m);
                    str.Append(" | ");
                    sb.Append(str.ToString());
                });
                sb.Append("]\n");
                sb.Append("[Board: ");
                current.BoardZone.ForEach(m =>
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
                    current.SecretZone.ForEach(s =>
                    {
                        if (s.IsQuest)
                            sb.Append($"{s}({s.QuestProgress})");
                        else
                            sb.Append(s);
                    });
                    sb.Append("]\n");
                }
                sb.Append("[Hand: ");
                current.HandZone.ForEach(p =>
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

        public static void DumpHistories(Game game, string filePath)
        {
            if (!game.History) return;

            using (FileStream file = File.OpenWrite(filePath))
            using (var writer = new StreamWriter(file))
            {
                // Create and write initialisation stream first.
                var initialisation = new GameServerStream
                {
                    MessageType = MsgType.InGame,
                    MessageState = true,
                    Message = JsonConvert.SerializeObject(new GameData
                    {
                        GameId = 10000,
                        PlayerId = 2,
                        GameDataType = GameDataType.Initialisation,
                        GameDataObject = JsonConvert.SerializeObject(
                            new List<UserInfo>
                            {
                                new UserInfo
                                {
                                    PlayerId = 1
                                },
                                new UserInfo
                                {
                                    PlayerId = 2
                                }
                            })
                    })
                };
                writer.WriteLine(JsonConvert.SerializeObject(initialisation));

                // Encode and write powerhistory entries
                string encodedHistories = JsonConvert.SerializeObject(game.PowerHistory.Full);
                var tempStreamObject = new GameServerStream
                {
                    MessageType = MsgType.InGame,
                    MessageState = true,
                    Message = JsonConvert.SerializeObject(new GameData
                    {
                        GameId = 10000,
                        PlayerId = 2,
                        GameDataType = GameDataType.PowerHistory,
                        GameDataObject = encodedHistories
                    })
                };
                writer.WriteLine(JsonConvert.SerializeObject(tempStreamObject));
            }
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
