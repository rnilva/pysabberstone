using SabberStoneContract.Model;
using SabberStoneCore.Kettle;
using System.Collections.Generic;
using SabberStoneCore.Model;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStoneContract.Interface
{
    public interface IGameAI
    {
        void InitialiseAgent();
        PlayerTask GetMove(Game game);
        PowerOptionChoice PowerOptions(Game game, List<PowerOption> powerOptionList);
        PowerChoices PowerChoices(Game game, PowerChoices powerChoices);
    }
}