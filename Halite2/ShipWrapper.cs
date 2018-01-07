using Halite2.hlt;

namespace Halite2
{
    public class ShipWrapper
    {
        public ShipWrapper(Ship ship)
        {
            Ship = ship;
        }

        public Ship Ship { get; set; }
        public ShipData Data { get; set; } = new ShipData();
    }
}
