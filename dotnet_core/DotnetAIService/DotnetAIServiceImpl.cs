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
        public const bool ConsoleOutput = true;

        private static Match CurrentMatch;

        public override Task<DotnetAIResponse> Request(DotnetAIRequest request, ServerCallContext context)
        {
            Console.WriteLine("Dotnet AI match request received");

            if (CurrentMatch != null)
            {
                Console.WriteLine("Only one match can be run. Aborted");
                return Task.FromResult(
                    new DotnetAIResponse
                    {
                        Type = DotnetAIResponse.Types.Type.Occupied
                    });
            } 

            try
            {
                // Get dotnet AI with a specified name
                IGameAI dotnetAI = FindAI.GetAI(request.DotnetAiName);
                Console.WriteLine($"... Found AI '{request.DotnetAiName}'");

                // Generate SabberStoneCore.Model.Game
                Game modelGame = SabberHelpers.GenerateGame(request.DotnetAiDeckstring,
                                                            request.PythonAiDeckstring,
                                                            request.History,
                                                            request.Seed);
                // Create a match instance
                CurrentMatch = new Match(dotnetAI, modelGame, request.DotnetAiName);

                if (ConsoleOutput)
                {
                    Console.WriteLine("########## Initial State #########");
                    SabberHelpers.Printers.PrintGame(modelGame);
                    Console.WriteLine("##################################");
                }

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
            Match match = CurrentMatch;

            try
            {
                match.ProcessPythonOption(request);

                if (match.IsCompleted()) 
                {
                    if (ConsoleOutput)
                    {
                        Console.WriteLine("Current match is completed!");
                    }

                    match.SaveHistory();

                    CurrentMatch = null;
                }
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                while (e.InnerException != null)
                {
                    e = e.InnerException;
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
                throw;
            }

            return Task.FromResult(new API.Game(match.CurrentPartiallyObservableGame, match.APIGameId));
        }
    }
}
