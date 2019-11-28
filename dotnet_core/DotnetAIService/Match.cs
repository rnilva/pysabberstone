using System;
using System.Collections.Generic;
using System.IO;
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
        public readonly string DotnetAgentName;

        private readonly Game _game;
        public int APIGameId { get; set; }

        public Game CurrentPartiallyObservableGame => CreatePartiallyObservableGame(_game);

        public Match(IGameAI dotnetAgent, Game game, string dotnetAgentName)
        {
            DotnetAgent = dotnetAgent;
            DotnetAgentName = dotnetAgentName;
            _game = game;

            // Run dotnetAgent if it is the first player
            GetDotnetAgentMoves();
        }

        public void ProcessPythonOption(API.Option apiOption)
        {
            PlayerTask playerTask = SabberHelpers.GetPlayerTask(apiOption, _game);

            if (DotnetAIServiceImpl.ConsoleOutput)
            {
                Console.WriteLine($"Option from python is received: {SabberHelpers.Printers.PrintAction(playerTask)}");
            }
            

            _game.Process(playerTask);

            if (_game.CurrentPlayer.PlayerId == 1)
                GetDotnetAgentMoves();
        }

        public bool IsCompleted()
        {
            return _game.State == State.COMPLETE;
        }

        public void SaveHistory()
        {
            if (!_game.History) return;

            const string HISTORY_DIR = "history";

            Directory.CreateDirectory(HISTORY_DIR);
            string fileName = $"{DotnetAgentName}_{DateTime.Now:ddmmyyyyHHMMSS}.log";

            string path = Path.Combine(HISTORY_DIR, fileName);

            SabberHelpers.DumpHistories(_game, path);
        }

        private void GetDotnetAgentMoves()
        {
            if (DotnetAIServiceImpl.ConsoleOutput)
            {
                Console.WriteLine(DotnetAgentName + "'s turn.");
            }

            while (_game.CurrentPlayer.PlayerId == 1 &&
                   _game.State != State.COMPLETE)
            {   
                var poGame = CreatePartiallyObservableGame(_game);
                _game.Process(DotnetAgent.GetMove(poGame));
            }

            if (DotnetAIServiceImpl.ConsoleOutput)
            {
                Console.WriteLine("############## State #############");
                Console.WriteLine(SabberHelpers.Printers.PrintGame(_game));
                Console.WriteLine("##################################");
            }
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
