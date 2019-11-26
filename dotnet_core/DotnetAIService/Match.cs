using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneContract.Interface;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStonePython.DotnetAIService
{
    public class Match
    {
        public readonly IGameAI DotnetAgent;
        public readonly IGameAI PythonAgent;
        //public readonly string DotnetAgentDeckstring;
        //public readonly string PythonAgentDeckstring;

        private readonly Game _game;
        public int APIGameId { get; set; }

        public Game CurrentPartiallyObservableGame => CreatePartiallyObservableGame(_game);

        public Match(IGameAI dotnetAgent, IGameAI pythonAgent, Game game)
        {
            DotnetAgent = dotnetAgent;
            PythonAgent = pythonAgent;
            _game = game;

            while (game.CurrentPlayer.PlayerId != 1)
            {   // Run dotnetAgent if it is the first player
                var poGame = CreatePartiallyObservableGame(game);
                game.Process(dotnetAgent.GetMove(poGame));
            }
        }

        public void ProcessPythonOption(API.Option apiOption)
        {
            PlayerTask playerTask = SabberHelpers.GetPlayerTask(apiOption, _game);
            _game.Process(playerTask);
        }

        public bool IsCompleted()
        {
            return _game.State == State.COMPLETE;
        }

        private static Game CreatePartiallyObservableGame(Game fullGame)
        {
            Game game = fullGame.Clone();
            SabberStoneCore.Model.Entities.Controller op = game.CurrentOpponent;
            SabberStoneCore.Model.Zones.HandZone hand = op.HandZone;
            ReadOnlySpan<IPlayable> span = hand.GetSpan();
            for (int i = span.Length - 1; i >= 0; --i)
            {
                hand.Remove(span[i]);
                hand.Add(new Unknown(in op, PlaceHolder, span[i].Id));
            }
            game.AuraUpdate();
            span = op.DeckZone.GetSpan();
            for (int i = 0; i < span.Length; i++)
                span[i].ActivatedTrigger?.Remove();
            var deck = new SabberStoneCore.Model.Zones.DeckZone(op);
            for (int i = 0; i < span.Length; i++)
            {
                span[i].ActivatedTrigger?.Remove();
                deck.Add(new Unknown(in op, PlaceHolder, span[i].Id));
            }
            op.DeckZone = deck;
            return game;
        }
        private static readonly Dictionary<GameTag, int> PlaceHolder = new Dictionary<GameTag, int>(0);
    }
}
