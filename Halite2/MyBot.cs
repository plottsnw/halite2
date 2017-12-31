using Halite2.hlt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Halite2
{
    public class MrBot
    {
        private static GameMap gameMap;
        private static int playerId;
        private static double maxTimeInTicks = TimeSpan.TicksPerSecond * 2;
        private static int turnCount = 0;

        private static List<Planet> unownedPlanets;
        private static List<Planet> enemyPlanets;
        private static List<Planet> myPlanets;
        private static List<Planet> dockableOwnedPlanets;

        private static List<Ship> undockedShips;
        private static List<Move> moveList;

        public static void Main(string[] args)
        {
            //Debugger.Break();
            Log.IsEnabled = false;

            try
            {
                Networking networking = new Networking();
                gameMap = networking.Initialize("MrBot");
                playerId = gameMap.GetMyPlayerId();

                try
                {
                    while (true)
                    {
                        PlayTurn();
                    }
                }
                catch (FormatException)
                {
                    //Game over
                }
            }
            catch (Exception e)
            {
                Log.LogMessage(e.ToString());
                throw;
            }
        }

        private static void PlayTurn()
        {
            DateTime turnStart = DateTime.UtcNow;
            turnCount++;
            Log.LogMessage($"Starting turn: {turnCount}");

            moveList = new List<Move>(500);
            gameMap.UpdateMap(Networking.ReadLineIntoMetadata());

            unownedPlanets = gameMap.GetAllPlanets().Values.Where(planet => !planet.IsOwned()).ToList();
            enemyPlanets = gameMap.GetAllPlanets().Values.Where(planet => planet.GetOwner() != playerId).ToList();
            myPlanets = gameMap.GetAllPlanets().Values.Where(planet => planet.GetOwner() == playerId).ToList();
            dockableOwnedPlanets = myPlanets.Where(planet => planet.GetDockedShips().Count < planet.GetDockingSpots()).ToList();

            //Log.LogMessage("Unowned planets:");
            //unownedPlanets.ForEach((p) => Log.LogMessage(p.GetId().ToString()));
            //Log.LogMessage("Dockable owned planets:");
            //dockableOwnedPlanets.ForEach((p) => Log.LogMessage(p.GetId().ToString()));
            //Log.LogMessage("Enemy owned planets:");
            //enemyPlanets.ForEach((p) => Log.LogMessage(p.GetId().ToString()));

            undockedShips = GetAllUndockedShips().ToList();
            List<Planet> planetsToBeDocked = new List<Planet>(3);

            if (turnCount < 10)
            {
                PlayEarlyGameTurn();
            }
            else if (turnCount >= 10 && turnCount < 50)
            {
                PlayMidGameTurn(turnStart);
            }
            else
            {
                //Only move ships that are close to planets to dock
                PlayLateGameTurn(turnStart);
            }

            Networking.SendMoves(moveList);
        }

        /// <summary>
        /// Split ships up to each go to their own planets. Helps with collisions in early game.
        /// </summary>
        private static void PlayEarlyGameTurn()
        {
            List<Ship> shipsWithoutCommands = undockedShips.ToList();

            for (int i = 0; i < undockedShips.Count; i++)
            {
                Ship ship = shipsWithoutCommands[0];
                Planet planet = GetClosestPlanetToShip(ship, unownedPlanets);

                ship = GetClosestShipToPlanet(planet, shipsWithoutCommands);

                if (ship.CanDock(planet))
                {
                    moveList.Add(new DockMove(ship, planet));
                }
                else
                {
                    ThrustMove newThrustMove = Navigation.NavigateShipToDock(gameMap, ship, planet, Constants.MAX_SPEED);
                    if (newThrustMove != null)
                    {
                        moveList.Add(newThrustMove);
                    }
                }

                unownedPlanets.Remove(planet);
                shipsWithoutCommands.Remove(ship);
            }
        }

        /// <summary>
        /// Prioritizes claiming and populating planets to capacity. Then attacks when all full.
        /// </summary>
        /// <param name="turnStart"></param>
        private static void PlayMidGameTurn(DateTime turnStart)
        {
            for (int i = 0; i < undockedShips.Count; i++)
            {
                Ship ship = undockedShips[i];

                if (unownedPlanets.Any())
                {
                    MoveToClosestPlanetAndDock(moveList, unownedPlanets, ship);
                }
                else if (dockableOwnedPlanets.Any())
                {
                    MoveToClosestPlanetAndDock(moveList, dockableOwnedPlanets, ship);
                }
                else if (enemyPlanets.Any())
                {
                    var planetAndDistance = GetClosestPlanetToShipWithDistance(ship, enemyPlanets);
                    var closestPlanet = planetAndDistance.Item1;
                    var distanceToPlanet = planetAndDistance.Item2;

                    ThrustMove newThrustMove = Navigation.NavigateShipTowardsTarget(gameMap, ship, closestPlanet, Constants.MAX_SPEED, distanceToPlanet > Constants.MAX_SPEED);
                    if (newThrustMove != null)
                    {
                        Log.LogMessage($"Sending ship {ship.GetId()} to destroy planet {closestPlanet.GetId()}");
                        moveList.Add(newThrustMove);
                    }
                }

                double turnDelta = DateTime.UtcNow.Ticks - turnStart.Ticks;
                double shipDelta = (turnDelta / (i + 1)) * 2d;

                if (shipDelta > maxTimeInTicks - turnDelta)
                {
                    Log.LogMessage($"Out of time on {i} out of {undockedShips.Count}");
                    break;
                }
            }
        }

        /// <summary>
        /// Looks for an unowned planet within 50 units to dock at. Otherwise attacks.
        /// </summary>
        /// <param name="turnStart"></param>
        private static void PlayLateGameTurn(DateTime turnStart)
        {
            for (int i = 0; i < undockedShips.Count; i++)
            {
                Ship ship = undockedShips[i];

                Planet bestPlanetChoice = GetClosestPlanetToShipWithinDistance(ship, unownedPlanets);
                
                if (bestPlanetChoice == null)
                {
                    bestPlanetChoice = GetClosestPlanetToShipWithinDistance(ship, dockableOwnedPlanets);
                }

                if (bestPlanetChoice != null)
                {
                    MoveToPlanetAndDock(moveList, bestPlanetChoice, ship);
                }
                else if (enemyPlanets.Any())
                {
                    var planetAndDistance = GetClosestPlanetToShipWithDistance(ship, enemyPlanets);
                    var closestPlanet = planetAndDistance.Item1;
                    var distanceToPlanet = planetAndDistance.Item2;

                    ThrustMove newThrustMove = Navigation.NavigateShipTowardsTarget(gameMap, ship, closestPlanet, Constants.MAX_SPEED, distanceToPlanet > Constants.MAX_SPEED);
                    if (newThrustMove != null)
                    {
                        Log.LogMessage($"Sending ship {ship.GetId()} to destroy planet {closestPlanet.GetId()}");
                        moveList.Add(newThrustMove);
                    }
                }

                double turnDelta = DateTime.UtcNow.Ticks - turnStart.Ticks;
                double shipDelta = (turnDelta / (i + 1)) * 2d;

                if (shipDelta > maxTimeInTicks - turnDelta)
                {
                    Log.LogMessage($"Out of time on {i} out of {undockedShips.Count}");
                    break;
                }
            }
        }

        private static void MoveToClosestPlanetAndDock(List<Move> moveList, List<Planet> unownedPlanets, Ship ship)
        {
            Planet closestPlanet = GetClosestPlanetToShip(ship, unownedPlanets);
            MoveToPlanetAndDock(moveList, closestPlanet, ship);
        }

        private static void MoveToPlanetAndDock(List<Move> moveList, Planet planet, Ship ship)
        {
            if (ship.CanDock(planet))
            {
                Log.LogMessage($"Docking ship {ship.GetId()} on planet {planet.GetId()}");
                moveList.Add(new DockMove(ship, planet));
            }
            else
            {
                ThrustMove newThrustMove = Navigation.NavigateShipToDock(gameMap, ship, planet, Constants.MAX_SPEED);
                if (newThrustMove != null)
                {
                    Log.LogMessage($"Sending ship {ship.GetId()} to move to planet {planet.GetId()}");
                    moveList.Add(newThrustMove);
                }
            }
        }

        private static IEnumerable<Ship> GetAllUndockedShips()
        {
            return gameMap.GetMyPlayer().GetShips().Values.Where(ship => ship.DockingStatus == DockingStatus.Undocked);
        }

        private static Planet GetClosestPlanetToShip(Ship ship, IEnumerable<Planet> planets)
        {
            return GetClosestPlanetToShipWithDistance(ship, planets).Item1;
        }

        private static Planet GetClosestPlanetToShipWithinDistance(Ship ship, IEnumerable<Planet> planets, int distance = 50)
        {
            return GetClosestPlanetToShipWithDistance(ship, planets, distance).Item1;
        }

        private static Tuple<Planet, double> GetClosestPlanetToShipWithDistance(Ship ship, IEnumerable<Planet> planets, int distance = -1)
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

        private static Ship GetClosestShipToPlanet(Planet planet, List<Ship> ships)
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
