using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneCore.Model;

namespace SabberStonePython
{
    public static class ManagedObjects
    {
        public static Dictionary<int, Game> Games = new Dictionary<int, Game>();
        public static Dictionary<int, Game> InitialGames = new Dictionary<int, Game>();
        public static Dictionary<int, API.Game> InitialGameAPIs = new Dictionary<int, API.Game>();
    }
}
