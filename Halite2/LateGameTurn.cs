using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
    /// <summary>
    /// Looks for an unowned planet within 50 units to dock at. Otherwise attacks.
    /// </summary>
    class LateGameTurn : Turn
    {
        public LateGameTurn(GameMap gameMap) : base(gameMap)
        {
        }

        public override List<Move> Play()
        {
            for (int i = 0; i < UndockedShips.Count; i++)
            {
                Ship ship = UndockedShips[i];

                Planet bestPlanetChoice = Navigation.GetClosestPlanetToShipWithinDistance(ship, UnownedPlanets);

                if (bestPlanetChoice == null)
                {
                    bestPlanetChoice = Navigation.GetClosestPlanetToShipWithinDistance(ship, DockableOwnedPlanets);
                }

                if (bestPlanetChoice != null)
                {
                    MoveToPlanetAndDock(MoveList, bestPlanetChoice, ship);
                }
                else if (EnemyPlanets.Any())
                {
                    AttackClosestEnemyPlanet(ship);
                }

                if (AreAboutToTimeOut(i)) break;
            }

            return MoveList;
        }
    }
}
