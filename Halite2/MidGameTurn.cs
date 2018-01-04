using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
    /// <summary>
    /// Prioritizes claiming and populating planets to capacity. Then attacks when all full.
    /// </summary>
    class MidGameTurn : Turn
    {
        public MidGameTurn(GameMap gameMap) : base(gameMap)
        {
        }

        public override List<Move> Play()
        {
            for (int i = 0; i < UndockedShips.Count; i++)
            {
                Ship ship = UndockedShips[i];

                if (UnownedPlanets.Any())
                {
                    MoveToClosestPlanetAndDock(MoveList, UnownedPlanets, ship);
                }
                else if (DockableOwnedPlanets.Any())
                {
                    MoveToClosestPlanetAndDock(MoveList, DockableOwnedPlanets, ship);
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
