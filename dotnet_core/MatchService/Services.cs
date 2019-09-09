using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using SabberStonePython.API;
using EmptyMessage = SabberStonePython.API.Empty;

namespace SabberStone_gRPC.MatchService
{
    public class Services : SabberStonePython.API.MatchService.MatchServiceBase
    {
        public readonly Dictionary<string, AIClient> Clients = new Dictionary<string, AIClient>();

        public override Task<EmptyMessage> Initialise(MatchRequest request, ServerCallContext context)
        {
            if (Clients.ContainsKey(request.Id))
                return Task.FromResult(new EmptyMessage());

            AIClient client = AIClient.Initialise(request.Ip, request.Port, request.Id, "");

            Clients.Add(request.Id, client);

            return Task.FromResult(new EmptyMessage());
        }

        public override Task<EmptyMessage> Queue(QueueRequest request, ServerCallContext context)
        {
            AIClient client = Clients[request.Id];
            client.Queue(SabberStoneContract.Core.GameType.Normal, request.Deckstring);

            return Task.FromResult(new EmptyMessage());
        }

        public override Task<Game> GetState(EmptyMessage request, ServerCallContext context)
        {
            AIClient client = Clients[context.RequestHeaders[0].Value];

            return Task.FromResult(client.GetState());
        }

        public override Task<EmptyMessage> SendOption(Option request, ServerCallContext context)
        {
            AIClient client = Clients[context.RequestHeaders[0].Value];

            client.SetOption(request);

            return Task.FromResult(new EmptyMessage());
        }
    }
}
