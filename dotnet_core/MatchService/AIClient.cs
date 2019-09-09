using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using SabberStoneContract.Client;
using SabberStoneContract.Core;
using SabberStoneContract.Interface;
using SabberStoneContract.Model;
using SabberStoneCore.Kettle;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStonePython;
using SabberStonePython.API;
using Game = SabberStoneCore.Model.Game;
using GameState = SabberStonePython.API.Game;

namespace SabberStone_gRPC.MatchService
{
    public class AIClient : GameClient
    {
        private readonly PythonController _pythonController;

        public AIClient(string ip, int port, PythonController gameController) : base(ip, port, gameController)
        {
            _pythonController = gameController;
        }

        public static AIClient Initialise(string ip, int port, string id, string pwd)
        {
            var client = new AIClient(ip, port, new PythonController());

            client.Connect();
            Console.WriteLine($"Client [{id}] is connected to the match server!");
            client.Register(id, pwd);

            return client;
        }

        public GameState GetState()
        {
            var stateTCS = new TaskCompletionSource<GameState>();
            var optionTCS = new TaskCompletionSource<Option>();
            _pythonController.SetTCS(stateTCS, optionTCS);

            return stateTCS.Task.Result;
        }

        public void SetOption(Option option)
        {
            _pythonController.SetOption(option);

            while (_pythonController.AIHandler.StateTCS != null)
                Thread.Sleep(1);
        }

        public override void CallGameClientState(GameClientState oldState, GameClientState newState)
        {
            switch (newState)
            {
                case GameClientState.None:
                    break;
                case GameClientState.Connected:
                    break;
                case GameClientState.Registered:
                    break;
                case GameClientState.Queued:
                    break;
                case GameClientState.Invited:
                    break;
                case GameClientState.InGame:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
            }
        }

        public override void OnMatchFinished()
        {
            _pythonController.OnMatchFinished();
        }
    }

    public class PythonController : GameController
    {
        public PythonAIHandler AIHandler { get; private set; }

        public PythonController() : base(new PythonAIHandler())
        {
            AIHandler = (PythonAIHandler) GameAI;
        }

        public void SetTCS(TaskCompletionSource<GameState> stateTCS, TaskCompletionSource<Option> optionTCS)
        {
            AIHandler.SetTCS(stateTCS, optionTCS);
        }

        public void OnMatchFinished()
        {
            AIHandler.StateTCS.SetResult(new GameState(_poGame));
        }

        public void SetOption(Option option)
        {
            AIHandler.OptionTCS.SetResult(option);
        }
    }

    public class PythonAIHandler : IGameAI
    {
        public TaskCompletionSource<GameState> StateTCS { get; set; }
        public TaskCompletionSource<Option> OptionTCS { get; set; }

        public Func<Game, PlayerTask> GetMoveHandle { get; set; }


        #region Implementation of IGameAI

        public void InitialiseAgent()
        {
            
        }

        public void SetTCS(TaskCompletionSource<GameState> stateTCS, TaskCompletionSource<Option> optionTCS)
        {
            StateTCS = stateTCS;
            OptionTCS = optionTCS;
        }

        public PlayerTask GetMove(Game game)
        {
            throw new NotImplementedException();
        }

        public PowerOptionChoice PowerOptions(Game game, List<PowerOption> powerOptionList)
        {
            while (StateTCS == null)
                Thread.Sleep(1);

            Console.WriteLine(SabberHelpers.Printers.PrintGame(game));

            StateTCS.SetResult(new GameState(game));

            Option option = OptionTCS.Task.Result;
            PlayerTask task = SabberHelpers.GetPlayerTask(option, game);

            Console.WriteLine(SabberHelpers.Printers.PrintAction(task));

            StateTCS = null;
            OptionTCS = null;

            return SabberStoneContract.Helper.SabberStoneConverter.CreatePowerOption(task);
        }

        public PowerChoices PowerChoices(Game game, PowerChoices powerChoices)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
