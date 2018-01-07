using System.Collections.Generic;

namespace Halite2
{
    public static class ShipRoster
    {
        public static Dictionary<int, ShipWrapper> Roster { get; } = new Dictionary<int, ShipWrapper>();
    }
}
