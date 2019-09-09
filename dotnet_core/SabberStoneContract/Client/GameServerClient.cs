using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Grpc.Core;
using SabberStoneContract.Core;

namespace SabberStoneContract.Client
{
    public class GameServerClient : GameServerService.GameServerServiceClient
    {
        public override AsyncUnaryCall<ServerReply> PingAsync(ServerRequest request, Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
        {
            return base.PingAsync(request, headers, deadline, cancellationToken);
        }

        public override AsyncUnaryCall<AuthReply> AuthenticationAsync(AuthRequest request, Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
        {
            return base.AuthenticationAsync(request, headers, deadline, cancellationToken);
        }

        public override AsyncDuplexStreamingCall<GameServerStream, GameServerStream> GameServerChannel(Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
        {
            return base.GameServerChannel(headers, deadline, cancellationToken);
        }

        public override AsyncUnaryCall<ServerReply> GameQueueAsync(QueueRequest request, Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
        {
            return base.GameQueueAsync(request, headers, deadline, cancellationToken);
        }

        public override AsyncUnaryCall<MatchGameReply> MatchGameAsync(MatchGameRequest request, Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
        {
            return base.MatchGameAsync(request, headers, deadline, cancellationToken);
        }

        public override AsyncUnaryCall<ServerReply> DisconnectAsync(ServerRequest request, Metadata headers = null, DateTime? deadline = null, CancellationToken cancellationToken = default)
        {
            return base.DisconnectAsync(request, headers, deadline, cancellationToken);
        }
    }
}
