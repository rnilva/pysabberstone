using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SabberStoneContract.Interface;
using SabberStoneContract.Model;
using SabberStoneCore.Kettle;
using SabberStoneCore.Model;
using SabberStoneCore.Tasks.PlayerTasks;

namespace SabberStoneContract.Interface
{
    public class RandomAI : IGameAI
    {
        private Random _random;

        public RandomAI()
        {
            _random = new Random();
        }

        public PowerChoices PowerChoices(Game game, PowerChoices powerChoices)
        {
            var powerChoicesId = _random.Next(powerChoices.Entities.Count);
            return new PowerChoices() { ChoiceType = powerChoices.ChoiceType, Entities = new List<int>() { powerChoices.Entities[powerChoicesId] } };
        }

        public void InitialiseAgent()
        {
            
        }

        public void InitialiseGame()
        {
            
        }

        public PlayerTask GetMove(Game game)
        {
            throw new NotImplementedException();
        }

        public PowerOptionChoice PowerOptions(Game game, List<PowerOption> powerOptionList)
        {
            var powerOptionId = _random.Next(powerOptionList.Count);
            var powerOption = powerOptionList.ElementAt(powerOptionId);
            var target = powerOption.MainOption?.Targets != null && powerOption.MainOption.Targets.Count > 0
                ? powerOption.MainOption.Targets.ElementAt(_random.Next(powerOption.MainOption.Targets.Count))
                : 0;
            var subOption = powerOption.SubOptions != null && powerOption.SubOptions.Count > 0
                ? _random.Next(powerOption.SubOptions.Count)
                : 0;
            return new PowerOptionChoice() { PowerOption = powerOption, Target = target, Position = 0, SubOption = subOption };
        }
    }
}