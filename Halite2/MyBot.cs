using Halite2.hlt;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        //private static StreamWriter writer;

        public static void Main(string[] args)
        {
            //Debugger.Break();

            //try
            {
                Networking networking = new Networking();
                gameMap = networking.Initialize("MrBot");
                playerId = gameMap.GetMyPlayerId();

                int totalGameTurns = 100 + (int)Math.Floor(Math.Sqrt(gameMap.GetWidth() * gameMap.GetHeight()));
                //Log.LogMessage("Total calculated turns: " + totalGameTurns);

                for (int i = 0; i < totalGameTurns; i++)
                {
                    PlayTurn();
                }
            }
            //catch (Exception e)
            //{
            //    Log.LogMessage(e);
            //    throw;
            //}
        }

        private static void PlayTurn()
        {
            DateTime turnStart = DateTime.UtcNow;
            turnCount++;
            //Log.LogMessage($"Starting turn: {turnCount}");

            moveList = new List<Move>(500);
            gameMap.UpdateMap(Networking.ReadLineIntoMetadata());

            unownedPlanets = gameMap.GetAllPlanets().Values.Where(planet => !planet.IsOwned()).ToList();
            enemyPlanets = gameMap.GetAllPlanets().Values.Where(planet => planet.GetOwner() != playerId).ToList();
            myPlanets = gameMap.GetAllPlanets().Values.Where(planet => planet.GetOwner() == playerId).ToList();
            dockableOwnedPlanets = myPlanets.Where(planet => planet.GetDockedShips().Count < planet.GetDockingSpots()).ToList();

            undockedShips = GetAllUndockedShips().ToList();
            List<Planet> planetsToBeDocked = new List<Planet>(3);

            if (turnCount < 10)
            {
                PlayEarlyGameTurn();
            }
            else
            {
                PlayLateGameTurn(turnStart);
            }

            Networking.SendMoves(moveList);
        }

        private static void PlayEarlyGameTurn()
        {
            List<Ship> shipsWithoutCommands = undockedShips.ToList();

            for (int i = 0; i < undockedShips.Count; i++)
            {
                //Log.LogMessage("Ship: " + i);
                Ship ship = shipsWithoutCommands[0];

                //Log.LogMessage("Dockable planets: " + unownedPlanets.Count);
                Planet planet = GetClosestPlanetToShip(ship, unownedPlanets);
                //Log.LogMessage("Closest planet: " + planet?.ToString());

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

        private static void PlayLateGameTurn(DateTime turnStart)
        {
            //Log.LogMessage("Unowned planets:");
            //unownedPlanets.ForEach((p) => Log.LogMessage(p.GetId().ToString()));
            //Log.LogMessage("Dockable owned planets:");
            //dockableOwnedPlanets.ForEach((p) => Log.LogMessage(p.GetId().ToString()));
            //Log.LogMessage("Enemy owned planets:");
            //enemyPlanets.ForEach((p) => Log.LogMessage(p.GetId().ToString()));

            //foreach (Ship ship in GetAllUndockedShips())
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

                    ThrustMove newThrustMove = Navigation.NavigateShipTowardsTarget(gameMap, ship, closestPlanet, Constants.MAX_SPEED, distanceToPlanet > Constants.MAX_SPEED, 2, .1);
                    if (newThrustMove != null)
                    {
                        //Log.LogMessage($"Sending ship {ship.GetId()} to destroy planet {closestPlanet.GetId()}");
                        moveList.Add(newThrustMove);
                    }
                }

                double turnDelta = DateTime.UtcNow.Ticks - turnStart.Ticks;
                double shipDelta = (turnDelta / (i + 1)) * 2d;

                if (shipDelta > maxTimeInTicks - turnDelta)
                {
                    //Log.LogMessage($"Out of time on {i} out of {undockedShips.Count}");
                    break;
                }
            }
        }

        private static void MoveToClosestPlanetAndDock(List<Move> moveList, List<Planet> unownedPlanets, Ship ship)
        {
            Planet closestPlanet = GetClosestPlanetToShip(ship, unownedPlanets);

            if (ship.CanDock(closestPlanet))
            {
                //Log.LogMessage($"Sending ship {ship.GetId()} to dock on planet {closestPlanet.GetId()}");
                moveList.Add(new DockMove(ship, closestPlanet));
            }
            else
            {
                ThrustMove newThrustMove = Navigation.NavigateShipToDock(gameMap, ship, closestPlanet, Constants.MAX_SPEED);
                if (newThrustMove != null)
                {
                    //Log.LogMessage($"Sending ship {ship.GetId()} to move to planet {closestPlanet.GetId()}");
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

        private static Tuple<Planet, double> GetClosestPlanetToShipWithDistance(Ship ship, IEnumerable<Planet> planets)
        {
            Planet closest = null;
            double closestDistance = 99999999d;
            double testDistance;

            foreach (Planet planet in planets)
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
