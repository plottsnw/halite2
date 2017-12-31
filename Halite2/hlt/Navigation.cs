using System;
using System.Linq;

namespace Halite2.hlt
{
    public class Navigation
    {
        public static ThrustMove NavigateShipToDock(
                GameMap gameMap,
                Ship ship,
                Entity dockTarget,
                int maxThrust)
        {
            Position targetPos = ship.GetClosestPoint(dockTarget);

            return NavigateShipTowardsTarget(gameMap, ship, targetPos, maxThrust);
        }

        public static ThrustMove NavigateShipTowardsTarget(
                GameMap gameMap,
                Ship ship,
                Position targetPos,
                int maxThrust,
                bool avoidObstacles = true,
                int maxCorrections = Constants.MAX_NAVIGATION_CORRECTIONS,
                double angularStepRad = Constants.NAVIGATION_CORRECTION_STEP)
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
    }
}
