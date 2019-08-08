using System;
using System.Linq;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using SabberStone_gRPC.MMF.Functions;
using SabberStoneCore.Enums;
using SabberStoneCore.Loader;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStonePython;

namespace SabberStone_gRPC.MMF
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public readonly struct Option
    {
        public static readonly int Size = Marshal.SizeOf<Option>();

        public readonly PlayerTaskType Type;
        public readonly int SourcePosition;
        public readonly int TargetPosition;
        public readonly int SubOption;
        public readonly int Choice;
    }

    public static class MarshalOptions
    {
        private static unsafe int* MarshalOption(PlayerTaskType type, int* ip,
            int sourcePosition = 0, 
            int targetPosition = 0,
            int subOption = 0)
        {
            *ip++ = (int) type;
            *ip++ = sourcePosition;
            *ip++ = targetPosition;
            *ip++ = subOption;
            ip++;
            return ip;
        }

        private static unsafe void MarshalChoice(int choice, ref int* ip)
        {
            *ip++ = (int) PlayerTaskType.CHOOSE;
            ip += 3;
            *ip++ = choice;
        }

        private static unsafe void MarshalEndTurn(ref int* ip)
        {
            *ip++ = (int) PlayerTaskType.END_TURN;
            ip += 4;
        }


        public static unsafe int Options(this SabberStoneCore.Model.Entities.Controller c, MemoryMappedFile mmf)
        {
            MemoryMappedViewAccessor view = mmf.CreateViewAccessor();
            byte* initPtr = null;
            view.SafeMemoryMappedViewHandle.AcquirePointer(ref initPtr);
            int* ip = (int*)initPtr;

            if (c.Choice != null)
            {
                if (c.Choice.ChoiceType == ChoiceType.GENERAL)
                    c.Choice.Choices.ForEach(i => MarshalChoice(i, ref ip));

                throw new NotImplementedException();
            }

            int controllerId = c.Id;

            MarshalEndTurn(ref ip);

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
					ip = GetPlayCardTasks(ip, handSpan[i]);
				else
				{
					IPlayable[] playables = handSpan[i].ChooseOnePlayables;
					for (int j = 1; j < 3; j++)
						ip = GetPlayCardTasks(ip, handSpan[i], playables[j - 1], j);
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
                        ip = MarshalOption(PlayerTaskType.HERO_POWER, ip);
                    else
                    {
                        ip = MarshalOption(PlayerTaskType.HERO_POWER, ip, 0, 0, 1);
                        ip = MarshalOption(PlayerTaskType.HERO_POWER, ip, 0, 0, 2);
                    }
                }
				else
				{
					if (heroPowerCard.IsPlayableByCardReq(c))
					{
						Character[] targets = GetTargets(heroPowerCard);
                        if (targets != null)
                            for (int i = 0; i < targets.Length; i++)
                                ip = MarshalOption(PlayerTaskType.HERO_POWER, ip, 0,
                                    GetPosition(targets[i], controllerId));
                        else
                            ip = MarshalOption(PlayerTaskType.HERO_POWER, ip);
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
                    ip = MarshalOption(PlayerTaskType.MINION_ATTACK, ip, j + 1,
                        GetEnemyPosition(attackTargets[i]));

                if (isOpHeroValidAttackTarget && !(minion.CantAttackHeroes || minion.AttackableByRush))
                    ip = MarshalOption(PlayerTaskType.MINION_ATTACK, ip, j + 1, OP_HERO_POSITION);
            }
			#endregion

			#region HeroAttackTaskts
			Hero hero = c.Hero;

			if ((!hero.IsExhausted || (hero.ExtraAttacksThisTurn > 0 && hero.ExtraAttacksThisTurn >= hero.NumAttacksThisTurn))
			    && hero.AttackDamage > 0 && !hero.IsFrozen)
			{
				GenerateAttackTargets();

                for (int i = 0; i < attackTargets.Length; i++)
                    ip = MarshalOption(PlayerTaskType.HERO_ATTACK, ip, 0, GetEnemyPosition(attackTargets[i]));

                if (isOpHeroValidAttackTarget && !hero.CantAttackHeroes)
                    ip = MarshalOption(PlayerTaskType.HERO_ATTACK, ip, 0, OP_HERO_POSITION);
            }
			#endregion

            return (int)((byte*)ip - initPtr);

			#region local functions
			int* GetPlayCardTasks(int* ptr , in IPlayable playable, in IPlayable chooseOnePlayable = null, int subOption = -1)
			{
				Card card = chooseOnePlayable?.Card ?? playable.Card;

				if (!spellCostHealth.HasValue)
					spellCostHealth = c.ControllerAuraEffects[GameTag.SPELLS_COST_HEALTH] == 1;

				bool healthCost = (playable.AuraEffects?.CardCostHealth ?? false) ||
				                  (spellCostHealth.Value && playable.Card.Type == CardType.SPELL);

				if (!healthCost && (playable.Cost > mana || playable.Card.HideStat))
					return ptr;

				// check PlayableByPlayer
				switch (playable.Card.Type)
				{
					//	REQ_MINION_CAP
					case CardType.MINION when c.BoardZone.IsFull:
						return ptr;
					case CardType.SPELL:
					{
						if (card.IsSecret)
						{
							if (c.SecretZone.IsFull) // REQ_SECRET_CAP
								return ptr;
							if (c.SecretZone.Any(p => p.Card.AssetId == card.AssetId)) // REQ_UNIQUE_SECRET
								return ptr;
						}

						if (card.IsQuest && c.SecretZone.Quest != null)
							return ptr;
						break;
					}
				}

				{
					if (!card.IsPlayableByCardReq(c))
						return ptr;

					Character[] targets = GetTargets(card);

                    int sourcePosition = playable.ZonePosition;

                    // Card doesn't require any targets
					if (targets == null)
                    {
                        if (playable is Minion)
                            for (int i = 0; i <= zonePosRange; i++)

                                ptr = MarshalOption(PlayerTaskType.PLAY_CARD, ptr, sourcePosition, i + 1, subOption);
                        else
                            ptr = MarshalOption(PlayerTaskType.PLAY_CARD, ptr, sourcePosition, 0, subOption);
                    }
					else
					{
						if (targets.Length == 0)
						{
							if (card.MustHaveTargetToPlay)
								return ptr;

							if (playable is Minion)
                                for (int i = 0; i <= zonePosRange; i++)
                                    ptr = MarshalOption(PlayerTaskType.PLAY_CARD, ptr, sourcePosition, i + 1, subOption);
							else
                                ptr = MarshalOption(PlayerTaskType.PLAY_CARD, ptr, sourcePosition, 0, subOption);
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
                                    ptr = MarshalOption(PlayerTaskType.PLAY_CARD, ptr, sourcePosition, 
                                        GetPosition(target, controllerId), subOption);
                            }
                        }
                    }
				}

                return ptr;
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

        public const int HERO_POSITION = 0;
        public const int OP_HERO_POSITION = 8;

        public static int GetPosition(ICharacter character, int controllerId)
        {
            if (character == null)
                return 0;

            if (character.Controller.Id != controllerId)
            {
                if (character.Card.Type == SabberStoneCore.Enums.CardType.MINION)
                    return character.ZonePosition + 9;
                else
                    return OP_HERO_POSITION;   // 8 for the opponent's Hero
            }
            else
            {
                if (character.Card.Type == SabberStoneCore.Enums.CardType.MINION)
                    return character.ZonePosition + 1;
                else
                    return HERO_POSITION;   // 0 for the player's Hero
            }
        }

        public static int GetFriendlyPosition(IPlayable character)
        {
            if (character.Card.Type == SabberStoneCore.Enums.CardType.MINION)
                return character.ZonePosition + 1;
            else
                return HERO_POSITION;
        }

        public static int GetEnemyPosition(ICharacter character)
        {
            if (character.Card.Type == SabberStoneCore.Enums.CardType.MINION)
                return character.ZonePosition + 9;
            else
                return OP_HERO_POSITION;
        }
    }
}
