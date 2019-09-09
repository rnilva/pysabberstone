using Newtonsoft.Json;
using SabberStoneContract.Helper;
using SabberStoneContract.Interface;
using SabberStoneContract.Model;
using SabberStoneCore.Config;
using SabberStoneCore.Kettle;
using SabberStoneCore.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SabberStoneContract.Core;
using SabberStoneCore.Enums;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStoneContract.Client
{
    public class GameController
    {
        private Action<MsgType, bool, string> _sendGameMessage;

        private List<UserInfo> _userInfos;
        private Game _originalGame;
        protected Game _poGame;
        public ConcurrentQueue<IPowerHistoryEntry> HistoryEntries { get; }

        public PowerChoices PowerChoices { get; set; }
        public PowerOptions PowerOptions { get; set; }
        protected IGameAI GameAI { get; }
        public int GameId { get; set; }

        public int PlayerId { get; set; }

        public UserInfo MyUserInfo => _userInfos.FirstOrDefault(p => p.PlayerId == PlayerId);

        public UserInfo OpUserInfo => _userInfos.FirstOrDefault(p => p.PlayerId != PlayerId);



        /// <summary>
        ///
        /// </summary>
        /// <param name="gameAI"></param>
        /// <param name="sendGameMessage"></param>
        public GameController(IGameAI gameAI)
        {
            _userInfos = new List<UserInfo>();

            GameAI = gameAI ?? new RandomAI();
            HistoryEntries = new ConcurrentQueue<IPowerHistoryEntry>();
            PowerOptions = null;
            PowerChoices = null;
        }

        internal void Reset()
        {
            _originalGame = null;

            GameId = 0;
            PlayerId = 0;

            _userInfos.Clear();
            while (!HistoryEntries.IsEmpty)
            {
                HistoryEntries.TryDequeue(out _);
            }
            PowerOptions = null;
            PowerChoices = null;
        }

        internal void SetSendGameMessage(Action<MsgType, bool, string> sendGameMessage)
        {
            _sendGameMessage = sendGameMessage;
        }

        internal void SetUserInfos(List<UserInfo> userInfos)
        {
            _userInfos = userInfos;

            var gameConfigInfo = userInfos[0].GameConfigInfo;

            _originalGame = SabberStoneConverter.CreateGame(userInfos[0], userInfos[1], gameConfigInfo);

            _originalGame.StartGame();
            _poGame = CreatePartiallyObservableGame(_originalGame);

            CallInitialisation();
        }

        protected virtual async void CallInitialisation()
        {
            await Task.Run(() =>
            {
            });
        }

        internal void SetPowerHistory(List<IPowerHistoryEntry> powerHistoryEntries)
        {
            powerHistoryEntries.ForEach(p => HistoryEntries.Enqueue(p));
            CallPowerHistory();
        }

        protected virtual async void CallPowerHistory()
        {
            await Task.Run(() =>
            {
            });
        }

        internal void SetPowerChoices(PowerChoices powerChoices)
        {
            PowerChoices = powerChoices;
            CallPowerChoices();
        }

        protected virtual async void CallPowerChoices()
        {
            await Task.Run(() =>
            {
                SendPowerChoicesChoice(GameAI.PowerChoices(_poGame, PowerChoices));
            });
        }

        internal void SetPowerChoice(int playerId, PowerChoices powerChoices)
        {
            var choiceTask = SabberStoneConverter.CreatePlayerTaskChoice(_originalGame, playerId, powerChoices.ChoiceType, powerChoices.Entities);

            _originalGame.Process(choiceTask);

            _poGame = CreatePartiallyObservableGame(_originalGame);
        }

        internal void SetPowerOptions(PowerOptions powerOptions)
        {
            PowerOptions = powerOptions;
            if (PowerOptions.PowerOptionList != null &&
                PowerOptions.PowerOptionList.Count > 0)
            {
                CallPowerOptions();
            }
        }

        protected virtual async void CallPowerOptions()
        {
            await Task.Run(() =>
            {
                SendPowerOptionChoice(GameAI.PowerOptions(_poGame, PowerOptions.PowerOptionList));
            });
        }

        internal void SetPowerOption(int playerId, PowerOptionChoice powerOptionChoice)
        {
            PlayerTask optionTask = SabberStoneConverter.CreatePlayerTaskOption(_originalGame, powerOptionChoice.PowerOption, powerOptionChoice.Target, powerOptionChoice.Position, powerOptionChoice.SubOption);

            _originalGame.Process(optionTask);

            _poGame = CreatePartiallyObservableGame(_originalGame);
        }

        internal void SetResult()
        {
            //Console.WriteLine($"{MyUserInfo.AccountName}: {_game.Hash()}");
        }

        public void SendInvitationReply(bool accept)
        {
            _sendGameMessage(MsgType.Invitation, accept,
                JsonConvert.SerializeObject(
                    new GameData
                    {
                        GameId = GameId,
                        PlayerId = PlayerId,
                        GameDataType = GameDataType.None
                    }));
        }

        private void SendPowerChoicesChoice(PowerChoices powerChoices)
        {
            PowerChoices = null;
            _sendGameMessage(MsgType.InGame, true,
                JsonConvert.SerializeObject(
                    new GameData()
                    {
                        GameId = GameId,
                        PlayerId = PlayerId,
                        GameDataType = GameDataType.PowerChoice,
                        GameDataObject = JsonConvert.SerializeObject(powerChoices)
                    }));
        }

        private void SendPowerOptionChoice(PowerOptionChoice powerOptionChoice)
        {
            PowerOptions = null;
            _sendGameMessage(MsgType.InGame, true,
                JsonConvert.SerializeObject(
                    new GameData()
                    {
                        GameId = GameId,
                        PlayerId = PlayerId,
                        GameDataType = GameDataType.PowerOption,
                        GameDataObject = JsonConvert.SerializeObject(powerOptionChoice)
                    }));
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
