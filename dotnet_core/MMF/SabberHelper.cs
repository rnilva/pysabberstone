using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks.PlayerTasks;
using static SabberStonePython.SabberHelpers;

namespace SabberStone_gRPC.MMF
{
    public static class SabberHelper
    {
        public static Game GenerateGame(string deckString1, string deckString2, bool startGame = true)
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

            if (startGame) game.StartGame();

            return game;
        }

        public static PlayerTask OptionToPlayerTask(SabberStoneCore.Model.Entities.Controller c, in Option option)
        {
            const bool SkipPrePhase = true;

            switch (option.Type)
            {
                case PlayerTaskType.CHOOSE:
                    return ChooseTask.Pick(c, option.Choice);
                case PlayerTaskType.CONCEDE:
                    return ConcedeTask.Any(c);
                case PlayerTaskType.END_TURN:
                    return EndTurnTask.Any(c);
                case PlayerTaskType.HERO_ATTACK:
                    return HeroAttackTask.Any(c, GetOpponentTarget(option.TargetPosition), SkipPrePhase);
                case PlayerTaskType.HERO_POWER:
                    return HeroPowerTask.Any(c, GetTarget(option.TargetPosition), option.SubOption, SkipPrePhase);
                case PlayerTaskType.MINION_ATTACK:
                    return MinionAttackTask.Any(c, c.BoardZone[option.SourcePosition - 1], GetOpponentTarget(option.TargetPosition),SkipPrePhase);
                case PlayerTaskType.PLAY_CARD:
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
                if (position == MarshalOptions.OP_HERO_POSITION) return c.Opponent.Hero;
                return c.Opponent.BoardZone[position - 9];
            }

            ICharacter GetTarget(int position)
            {
                if (position == 0)
                    return null;
                if (position >= MarshalOptions.OP_HERO_POSITION)
                    return GetOpponentTarget(position);
                if (position == MarshalOptions.HERO_POSITION)
                    return c.Hero;
                return c.BoardZone[position - 1];
            }
        }
    }
}
