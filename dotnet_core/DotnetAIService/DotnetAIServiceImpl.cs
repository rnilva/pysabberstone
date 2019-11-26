using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using SabberStone_gRPC.MatchService;
using SabberStoneContract.Interface;
using SabberStonePython.API;
using Game = SabberStoneCore.Model.Game;

namespace SabberStonePython.DotnetAIService
{
    public class DotnetAIServiceImpl : API.DotnetAIService.DotnetAIServiceBase
    {
        private static Match CurrentMatch;

        public override Task<DotnetAIResponse> Request(DotnetAIRequest request, ServerCallContext context)
        {
            if (CurrentMatch != null) return Task.FromResult(
                new DotnetAIResponse
                {
                    Type = DotnetAIResponse.Types.Type.Occupied
                });

            try
            {
                // Get dotnet AI with a specified name
                IGameAI dotnetAI = FindAI.GetAI(request.DotnetAiName);

                // Generate SabberStoneCore.Model.Game
                Game modelGame = SabberHelpers.GenerateGame(request.DotnetAiDeckstring, request.PythonAiDeckstring);

                // Create a pythonAI instance
                IGameAI pythonAI = new PythonAIHandler();

                // Create a match instance
                CurrentMatch = new Match(dotnetAI, pythonAI, modelGame);
                var apiGame = new API.Game(CurrentMatch.CurrentPartiallyObservableGame);
                CurrentMatch.APIGameId = apiGame.Id.Value;

                // Return Current Partially Observable Game
                return Task.FromResult(new DotnetAIResponse
                {
                    Type = DotnetAIResponse.Types.Type.Success,
                    Game = apiGame
                });

            }
            catch (FindAI.AINotFoundException)
            {
                return Task.FromResult(new DotnetAIResponse
                {
                    Type = DotnetAIResponse.Types.Type.NotFound
                });
            }
            catch (Exception)
            {
                return Task.FromResult(new DotnetAIResponse
                {
                    Type = DotnetAIResponse.Types.Type.InvalidDeckstring
                });
            }
        }

        public override Task<API.Game> SendPythonAIOption(Option request, ServerCallContext context)
        {
            var match = CurrentMatch;

            match.ProcessPythonOption(request);
            if (match.IsCompleted()) 
                CurrentMatch = null;

            return Task.FromResult(new API.Game(match.CurrentPartiallyObservableGame, match.APIGameId));
        }
    }
}
