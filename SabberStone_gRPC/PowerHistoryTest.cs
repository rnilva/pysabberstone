using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using SabberStoneCore;
using SabberStoneCore.Enums;
using SabberStoneCore.Kettle;

namespace SabberStone_gRPC
{
    public static class PowerHistoryTest
    {
        public static void Test()
        {

        }
    }

    public class PowerHistoryImpl : SabberStoneRPCImpl
    {
        public static PowerHistory GetMessage(IPowerHistoryEntry kettleHistory)
        {
            switch (kettleHistory)
            {
                case PowerHistoryBlockEnd powerHistoryBlockEnd:
                    return new PowerHistory
                    {
                        PowerEnd = new PowerHistoryEnd()
                    };
                case PowerHistoryBlockStart powerHistoryBlockStart:
                    return new PowerHistory
                    {
                        PowerStart = new PowerHistoryStart
                        {
                            Type = (HistoryBlockType)powerHistoryBlockStart.BlockType,
                            Index = powerHistoryBlockStart.Index,
                            Source = powerHistoryBlockStart.Target,
                            EffectCardId = powerHistoryBlockStart.EffectCardId
                        }
                    };
                case SabberStoneCore.Kettle.PowerHistoryCreateGame powerHistoryCreateGame:
                    return new PowerHistory
                    {
                        CreateGame = new PowerHistoryCreateGame
                        {
                            Players = new Google.Protobuf.Collections.RepeatedField<Player>(),

                        }
                    }
                    break;
                case PowerHistoryFullEntity powerHistoryFullEntity:
                    break;
                case PowerHistoryHideEntity powerHistoryHideEntity:
                    break;
                case SabberStoneCore.Kettle.PowerHistoryMetaData powerHistoryMetaData:
                    break;
                case PowerHistoryShowEntity powerHistoryShowEntity:
                    break;
                case SabberStoneCore.Kettle.PowerHistoryTagChange powerHistoryTagChange:
                    break;
            }
        }

        public static PowerHistory PowerHistoryBuilder(PowerHistoryType type)
        {
            switch (type)
            {
                case PowerHistoryType.PowerhistoryInvalid:
                    throw new ArgumentException();
                case PowerHistoryType.PowerhistoryFullEntity:
                    return new PowerHistory {Type = type, FullEntity = new PowerHistoryEntity()};
                case PowerHistoryType.PowerhistoryShowEntity:
                    return new PowerHistory {Type = type, ShowEntity = new PowerHistoryEntity()};
                case PowerHistoryType.PowerhistoryHideEntity:
                    return new PowerHistory {Type = type, HideEntity = new PowerHistoryHide()};
                case PowerHistoryType.PowerhistoryTagChange:
                    break;
                case PowerHistoryType.PowerhistoryCreateGame:
                    break;
                case PowerHistoryType.PowerhistoryPowerStart:
                    break;
                case PowerHistoryType.PowerhistoryPowerEnd:
                    break;
                case PowerHistoryType.PowerhistoryMetaData:
                    break;
                case PowerHistoryType.PowerhistoryChangeEntity:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public override Task GetHistories(Empty request, IServerStreamWriter<PowerHistory> responseStream, ServerCallContext context)
        {
            var types = Enum.GetValues(typeof(PowerHistoryType));
            var rnd = new Random();

            var powerHistory = new PowerHistory();

            powerHistory.PowerHistoryCase

            for (int i = 0; i < 30; i++)
            {
                var type = (PowerHistoryType)types.GetValue(rnd.Next(types.Length));

                
            }
        }
    }
}
