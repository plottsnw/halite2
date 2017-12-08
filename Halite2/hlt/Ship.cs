namespace Halite2.hlt
{
    public enum DockingStatus { Undocked = 0, Docking = 1, Docked = 2, Undocking = 3 }
    public enum ShipType { NotSet = 0, Miner = 1, Attack = 2 }

    public class Ship : Entity
    {
        public Ship(int owner, int id, double xPos, double yPos,
                    int health, DockingStatus dockingStatus, int dockedPlanet,
                    int dockingProgress, int weaponCooldown)
            : base(owner, id, xPos, yPos, health, Constants.SHIP_RADIUS)
        {
            DockingStatus = dockingStatus;
            DockedPlanet = dockedPlanet;
            DockingProgress = dockingProgress;
            WeaponCooldown = weaponCooldown;
        }

        public ShipType Type { get; set; }

        public int WeaponCooldown { get; private set; }

        public DockingStatus DockingStatus { get; private set; }

        public int DockingProgress { get; private set; }

        public int DockedPlanet { get; set; }

        public bool CanDock(Planet planet)
        {
            return GetDistanceTo(planet) <= Constants.SHIP_RADIUS + Constants.DOCK_RADIUS + planet.GetRadius();
        }

        public override string ToString()
        {
            return "Ship[" +
                    base.ToString() +
                    ", dockingStatus=" + DockingStatus +
                    ", dockedPlanet=" + DockedPlanet +
                    ", dockingProgress=" + DockingProgress +
                    ", weaponCooldown=" + WeaponCooldown +
                    "]";
        }
    }
}
