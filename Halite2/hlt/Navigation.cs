using System;
using System.Collections.Generic;
using System.Linq;

namespace Halite2.hlt
{
    public static class Navigation
    {
        public static ThrustMove NavigateShipToDock(GameMap gameMap, Ship ship, Entity dockTarget, int maxThrust)
        {
            Position targetPos = ship.GetClosestPoint(dockTarget);

            return NavigateShipTowardsTarget(gameMap, ship, targetPos, maxThrust);
        }

        public static ThrustMove NavigateShipTowardsTarget(GameMap gameMap, Ship ship, Position targetPos, int maxThrust, bool avoidObstacles = true,
                int maxCorrections = Constants.MAX_NAVIGATION_CORRECTIONS, double angularStepRad = Constants.NAVIGATION_CORRECTION_STEP)
        {
            if (maxCorrections <= 0)
            {
                Log.LogMessage($"Pathfinding failed for ship {ship.GetId()}");
                return null;
            }

            double distance = ship.GetDistanceTo(targetPos);
            double angleRad = ship.OrientTowardsInRad(targetPos);

            if (avoidObstacles && gameMap.ObjectsBetween(ship, targetPos).Any())
            {
                double newTargetDx = Math.Cos(angleRad + angularStepRad) * distance;
                double newTargetDy = Math.Sin(angleRad + angularStepRad) * distance;
                Position newTarget = new Position(ship.GetXPos() + newTargetDx, ship.GetYPos() + newTargetDy);

                return NavigateShipTowardsTarget(gameMap, ship, newTarget, maxThrust, true, (maxCorrections - 1));
            }

            int thrust;
            if (distance < maxThrust)
            {
                // Do not round up, since overshooting might cause collision.
                thrust = (int)distance;
            }
            else
            {
                thrust = maxThrust;
            }

            int angleDeg = Util.AngleRadToDegClipped(angleRad);

            return new ThrustMove(ship, angleDeg, thrust);
        }

        public static Planet GetClosestPlanetToShip(Ship ship, IEnumerable<Planet> planets)
        {
            return GetClosestPlanetToShipWithDistance(ship, planets).Item1;
        }

        public static Planet GetClosestPlanetToShipWithinDistance(Ship ship, IEnumerable<Planet> planets, int distance = 50)
        {
            return GetClosestPlanetToShipWithDistance(ship, planets, distance).Item1;
        }

        public static Tuple<Planet, double> GetClosestPlanetToShipWithDistance(Ship ship, IEnumerable<Planet> planets, int distance = -1)
        {
            IEnumerable<Planet> filteredPlanets = planets;

            if (distance > -1)
            {
                filteredPlanets = planets.Where((p) => ship.GetDistanceTo(p) < distance);
            }

            Planet closest = null;
            double closestDistance = 99999999d;
            double testDistance;

            foreach (Planet planet in filteredPlanets)
            {
                testDistance = ship.GetDistanceTo(planet);

                if (testDistance < closestDistance)
                {
                    closest = planet;
                    closestDistance = testDistance;
                }
            }

            return new Tuple<Planet, double>(closest, closestDistance);
        }

        public static Ship GetClosestShipToPlanet(Planet planet, List<Ship> ships)
        {
            Ship closest = null;
            double closestDistance = 99999999d;
            double testDistance;

            foreach (Ship ship in ships)
            {
                testDistance = planet.GetDistanceTo(ship);

                if (testDistance < closestDistance)
                {
                    closest = ship;
                    closestDistance = testDistance;
                }
            }

            return closest;
        }
    }
}
