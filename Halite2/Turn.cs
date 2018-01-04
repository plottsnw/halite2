using Halite2.hlt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Halite2
{
    public abstract class Turn
    {
        public const double MAX_TIME_IN_TICKS = TimeSpan.TicksPerSecond * 2;

        protected GameMap GameMap { get; private set; }
        protected DateTime TurnStart { get; private set; }

        protected List<Planet> UnownedPlanets { get; private set; }
        protected List<Planet> EnemyPlanets { get; private set; }
        protected List<Planet> OwnedPlanets { get; private set; }
        protected List<Planet> DockableOwnedPlanets { get; private set; }

        protected List<Ship> UndockedShips { get; private set; }

        public List<Move> MoveList { get; private set; }

        public abstract List<Move> Play();

        public Turn(GameMap gameMap)
        {
            GameMap = gameMap;
        }

        public void RefreshGameData()
        {
            TurnStart = DateTime.UtcNow;

            MoveList = new List<Move>(500);
            GameMap.UpdateMap(Networking.ReadLineIntoMetadata());

            UnownedPlanets = GameMap.GetAllPlanets().Values.Where(planet => !planet.IsOwned()).ToList();
            EnemyPlanets = GameMap.GetAllPlanets().Values.Where(planet => planet.GetOwner() != GameMap.GetMyPlayerId()).ToList();
            OwnedPlanets = GameMap.GetAllPlanets().Values.Where(planet => planet.GetOwner() == GameMap.GetMyPlayerId()).ToList();
            DockableOwnedPlanets = OwnedPlanets.Where(planet => planet.GetDockedShips().Count < planet.GetDockingSpots()).ToList();

            UndockedShips = GetAllUndockedShips();

            //Log.LogMessage("Unowned planets:");
            //unownedPlanets.ForEach((p) => Log.LogMessage(p.GetId().ToString()));
            //Log.LogMessage("Dockable owned planets:");
            //dockableOwnedPlanets.ForEach((p) => Log.LogMessage(p.GetId().ToString()));
            //Log.LogMessage("Enemy owned planets:");
            //enemyPlanets.ForEach((p) => Log.LogMessage(p.GetId().ToString()));
        }

        protected void AttackClosestEnemyPlanet(Ship ship)
        {
            var planetAndDistance = Navigation.GetClosestPlanetToShipWithDistance(ship, EnemyPlanets);
            var closestPlanet = planetAndDistance.Item1;
            var distanceToPlanet = planetAndDistance.Item2;

            ThrustMove newThrustMove = Navigation.NavigateShipTowardsTarget(GameMap, ship, closestPlanet, Constants.MAX_SPEED, distanceToPlanet > Constants.MAX_SPEED);
            if (newThrustMove != null)
            {
                Log.LogMessage($"Sending ship {ship.GetId()} to destroy planet {closestPlanet.GetId()}");
                MoveList.Add(newThrustMove);
            }
        }

        protected void AttackDockedShipsAtClosestEnemyPlanet(Ship ship)
        {
            Planet closestEnemyPlanet = Navigation.GetClosestPlanetToShip(ship, EnemyPlanets);
            var closestDockedEnemyShips = closestEnemyPlanet.GetDockedShips().Select(id => GameMap.GetShip(closestEnemyPlanet.GetOwner(), id));

            if (closestDockedEnemyShips.Any())
            {
                Log.LogMessage($"closestDockedEnemyShips length: {closestDockedEnemyShips.Count()}");
                Tuple<Ship, double> closestDockedEnemyShipAndDistance = ship.GetClosestEntityFromListWithDistance(closestDockedEnemyShips);
                int thrust = GetThrustToAttackShip(closestDockedEnemyShipAndDistance.Item2);

                ThrustMove newThrustMove = Navigation.NavigateShipTowardsTarget(GameMap, ship, closestDockedEnemyShipAndDistance.Item1, thrust);
                if (newThrustMove != null)
                {
                    Log.LogMessage($"Sending ship {ship.GetId()} to destroy enemy docked ship {closestDockedEnemyShipAndDistance.Item1.GetId()}");
                    MoveList.Add(newThrustMove);
                } 
            }
        }

        private int GetThrustToAttackShip(double distanceBetweenShips)
        {
            int thrust = Constants.MAX_SPEED;

            if (distanceBetweenShips < Constants.MAX_SPEED)
            {
                thrust = (int)distanceBetweenShips - 2;
            }

            return thrust;
        }

        protected bool AreAboutToTimeOut(int turnCount)
        {
            double turnDelta = DateTime.UtcNow.Ticks - TurnStart.Ticks;
            double shipDelta = (turnDelta / (turnCount + 1)) * 2d; //multiply by 2 to add an extra turn of buffer

            if (shipDelta > MAX_TIME_IN_TICKS - turnDelta)
            {
                Log.LogMessage($"Out of time on {turnCount} out of {UndockedShips.Count}");
                return true;
            }

            return false;
        }

        private List<Ship> GetAllUndockedShips()
        {
            return GameMap.GetMyPlayer().GetShips().Values.Where(ship => ship.DockingStatus == DockingStatus.Undocked).ToList();
        }

        protected void MoveToClosestPlanetAndDock(List<Move> moveList, List<Planet> unownedPlanets, Ship ship)
        {
            Planet closestPlanet = Navigation.GetClosestPlanetToShip(ship, unownedPlanets);
            MoveToPlanetAndDock(moveList, closestPlanet, ship);
        }

        protected void MoveToPlanetAndDock(List<Move> moveList, Planet planet, Ship ship)
        {
            if (ship.CanDock(planet))
            {
                Log.LogMessage($"Docking ship {ship.GetId()} on planet {planet.GetId()}");
                moveList.Add(new DockMove(ship, planet));
            }
            else
            {
                ThrustMove newThrustMove = Navigation.NavigateShipToDock(GameMap, ship, planet, Constants.MAX_SPEED);
                if (newThrustMove != null)
                {
                    Log.LogMessage($"Sending ship {ship.GetId()} to move to planet {planet.GetId()}");
                    moveList.Add(newThrustMove);
                }
            }
        }
    }
}
