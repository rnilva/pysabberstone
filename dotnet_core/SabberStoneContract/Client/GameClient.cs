using Grpc.Core;
//using log4net;
using Newtonsoft.Json;
using SabberStoneContract.Client;
using SabberStoneContract.Helper;
using SabberStoneContract.Model;
using SabberStoneCore.Kettle;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using static SabberStoneContract.Core.GameServerService;

namespace SabberStoneContract.Core
{
    public class GameClient
    {
        //private static readonly ILog Log = Logger.Instance.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _url;

        private readonly string _target;

        private Channel _channel;

        private GameServerServiceClient _client;

        private GameClientState _gameClientState;

        private int _sessionId;

        private string _sessionToken;

        private readonly CancellationTokenSource _cancellationTokenSource;

        private ConcurrentQueue<GameServerStream> _gameServerStreamQueue;

        private GameController _gameController;

        public string AccountName { get; private set; }
        private string _password;

        public GameClientState GameClientState
        {
            get => _gameClientState;
            private set
            {
                var oldValue = _gameClientState;
                _gameClientState = value;
                CallGameClientState(oldValue, value);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="port"></param>
        /// <param name="sabberStoneAI"></param>
        public GameClient(string ip, int port, GameController gameController)
        {
            _url = $"{ip}:{port}";

            _cancellationTokenSource = new CancellationTokenSource();

            _gameServerStreamQueue = new ConcurrentQueue<GameServerStream>();

            _gameController = gameController;
            _gameController.SetSendGameMessage(SendGameMessage);

            GameClientState = GameClientState.None;
        }

        public void Connect()
        {
            _channel = new Channel(_url, ChannelCredentials.Insecure);
            _client = new GameServerServiceClient(_channel);

            try
            {
                int timeoutSeconds = 5;
                var serverReply = _client.Ping(new ServerRequest { Message = "Ping", },
				deadline: DateTime.UtcNow.AddSeconds(timeoutSeconds));
                if (serverReply.RequestState)
                {
                    GameClientState = GameClientState.Connected;
                    return;
                }
            }
            catch (RpcException exception)
            {
                if (exception.StatusCode != StatusCode.Unavailable)
                {
                    //Log.Error(exception.ToString());
                    throw;
                }
            }

            _channel.ShutdownAsync().Wait();
            return;
        }

        public void Register(string accountName, string accountPsw)
        {
            if (GameClientState != GameClientState.Connected)
            {
                //Log.Warn("Client isn't connected.");
                return;
            }

            var authReply = _client.Authentication(new AuthRequest { AccountName = accountName, AccountPsw = accountPsw });

            if (!authReply.RequestState)
            {
                //Log.Warn("Bad RegisterRequest.");
                return;
            }

            _sessionId = authReply.SessionId;
            _sessionToken = authReply.SessionToken;

            GameServerChannelAsync();

            GameClientState = GameClientState.Registered;

            AccountName = accountName;
            _password = accountPsw;
        }

        public void MatchGame()
        {
            if (GameClientState != GameClientState.InGame)
            {
                //Log.Warn("Client isn't in a game.");
                return;
            }

            var matchGameReply = _client.MatchGame(new MatchGameRequest { GameId = _gameController.GameId },
			new Metadata { new Metadata.Entry("token", _sessionToken) });

            if (!matchGameReply.RequestState)
            {
                //Log.Warn("Bad MatchGameRequest.");
                return;
            }
        }

        private async void GameServerChannelAsync()
        {
            using (var call = _client.GameServerChannel(
			headers: new Metadata { new Metadata.Entry("token", _sessionToken) },
			cancellationToken: _cancellationTokenSource.Token))
            {
                var requestStreamWriterTask = new Task(async () =>
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        if (_gameServerStreamQueue.TryDequeue(out GameServerStream gameServerStream))
                        {
                            Console.WriteLine($"[{AccountName}] sends [{gameServerStream.MessageType}]");
                            await call.RequestStream.WriteAsync(gameServerStream);
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                    }
                });
                requestStreamWriterTask.Start();

                await Task.Run(async () =>
                {
                    try
                    {
                        while (await call.ResponseStream.MoveNext(_cancellationTokenSource.Token))
                        {
                            Console.WriteLine($"[{AccountName}] receives [{call.ResponseStream.Current.MessageType}]");
                            ProcessChannelMessage(call.ResponseStream.Current);
                        }
                    }
                    catch (RpcException exception)
                    {
                        if (exception.StatusCode != StatusCode.Cancelled)
                        {
                            //Log.Error(exception.ToString());
                            throw exception;
                        }
                    }
                });
            }
        }

        public void SendGameMessage(MsgType msgType, bool msgState, string msgData)
        {
            _gameServerStreamQueue.Enqueue(new GameServerStream
            {
                MessageType = msgType,
                MessageState = msgState,
                Message = msgData
            });
        }

        public void Disconnect()
        {
            if (GameClientState == GameClientState.None || _channel == null )
            {
                return;
            }

            try
            {
                int timeoutSeconds = 5;
                var serverReply = _client.Disconnect(new ServerRequest(),
				new Metadata { new Metadata.Entry("token", _sessionToken ?? string.Empty) },
				DateTime.UtcNow.AddSeconds(timeoutSeconds));
                if (serverReply.RequestState)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            catch (RpcException exception)
            {
                if (exception.StatusCode != StatusCode.Unavailable)
                {
                    //Log.Error(exception.ToString());
                    throw exception;
                }
            }

            _channel.ShutdownAsync().Wait();

            GameClientState = GameClientState.None;
        }

        private void ProcessChannelMessage(GameServerStream current)
        {
            if (!current.MessageState)
            {
                //Log.Warn($"Failed messageType {current.MessageType}, '{current.Message}'!");
                return;
            }

            GameData gameData = null;
            if (current.Message != string.Empty)
            {
                gameData = JsonConvert.DeserializeObject<GameData>(current.Message);
                //Log.Info($"GameData[Id:{gameData.GameId},Player:{gameData.PlayerId}]: {gameData.GameDataType} received");
            }
            else
            {
                //Log.Info($"Message[{current.MessageState},{current.MessageType}]: received.");
            }

            Console.WriteLine($"[{AccountName}] receives GameData [{gameData.GameDataType}]");

            switch (current.MessageType)
            {
                //                case MsgType.Initialisation:
                //                    break;

                case MsgType.Invitation:

                    // preparing for a new game
                    _gameController.Reset();

                    _gameController.GameId = gameData.GameId;
                    _gameController.PlayerId = gameData.PlayerId;
                    GameClientState = GameClientState.Invited;

                    // action call here
                    CallInvitation();
                    break;

                case MsgType.InGame:
                    switch (gameData.GameDataType)
                    {
                        case GameDataType.Initialisation:
                            _gameController.SetUserInfos(
							JsonConvert.DeserializeObject<List<UserInfo>>(gameData.GameDataObject));
                            GameClientState = GameClientState.InGame;
                            break;

                        case GameDataType.PowerHistory:
                            _gameController.SetPowerHistory(
							JsonConvert.DeserializeObject<List<IPowerHistoryEntry>>(gameData.GameDataObject,
							new PowerHistoryConverter()));
                            break;

                        case GameDataType.PowerChoices:
                            _gameController.SetPowerChoices(
							JsonConvert.DeserializeObject<PowerChoices>(gameData.GameDataObject));
                            break;

                        case GameDataType.PowerChoice:
                            _gameController.SetPowerChoice(gameData.PlayerId,
							JsonConvert.DeserializeObject<PowerChoices>(gameData.GameDataObject));
                            break;

                        case GameDataType.PowerOptions:
                            _gameController.SetPowerOptions(
							JsonConvert.DeserializeObject<PowerOptions>(gameData.GameDataObject));
                            break;

                        case GameDataType.PowerOption:
                            _gameController.SetPowerOption(gameData.PlayerId,
							JsonConvert.DeserializeObject<PowerOptionChoice>(gameData.GameDataObject));
                            break;

                        case GameDataType.Result:
                            GameClientState = GameClientState.Registered;
                            _gameController.SetResult();
                            OnMatchFinished();
                            break;
                    }
                    break;
            }
        }

        public void Queue(GameType gameType = GameType.Normal, string deckData = "")
        {
            if (GameClientState != GameClientState.Registered)
            {
                //Log.Warn("Client isn't registred.");
                return;
            }

            ServerReply queueReply = _client.GameQueue(
                new QueueRequest
                {
                    GameType = gameType,
                    DeckData = deckData
                },
                new Metadata
				{
                    new Metadata.Entry("token", _sessionToken)
            });

            if (!queueReply.RequestState)
            {
                //Log.Warn("Bad QueueRequest.");
                return;
            }

            GameClientState = GameClientState.Queued;
        }

        public async virtual void CallGameClientState(GameClientState oldState, GameClientState newState)
        {
            await Task.Run(() =>
            {
                //Log.Info($"GameClientStateChange: {oldState} -> {newState}");
            });
        }

        public async virtual void CallInvitation()
        {
            await Task.Run(() => { _gameController.SendInvitationReply(true); });
        }

        public virtual void OnMatchFinished()
        {
        }
    }
}
