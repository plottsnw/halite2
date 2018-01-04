using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Halite2.hlt;

namespace Halite2
{
    /// <summary>
    /// Split ships up to each go to their own planets. Helps with collisions in early game.
    /// </summary>
    public class EarlyGameTurn : Turn
    {
        public EarlyGameTurn(GameMap gameMap) : base(gameMap)
        {

        }

        public override List<Move> Play()
        {
            List<Ship> shipsWithoutCommands = UndockedShips.ToList();

            for (int i = 0; i < UndockedShips.Count; i++)
            {
                Ship ship = shipsWithoutCommands[0];
                Planet planet = Navigation.GetClosestPlanetToShip(ship, UnownedPlanets);

                ship = Navigation.GetClosestShipToPlanet(planet, shipsWithoutCommands);

                if (ship.CanDock(planet))
                {
                    MoveList.Add(new DockMove(ship, planet));
                }
                else
                {
                    ThrustMove newThrustMove = Navigation.NavigateShipToDock(GameMap, ship, planet, Constants.MAX_SPEED);
                    if (newThrustMove != null)
                    {
                        MoveList.Add(newThrustMove);
                    }
                }

                UnownedPlanets.Remove(planet);
                shipsWithoutCommands.Remove(ship);
            }

            return MoveList;
        }
    }
}