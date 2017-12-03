using Halite2.hlt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Halite2
{
    public class MyBot
    {
        private static GameMap gameMap;
        private static int playerId;
        private static double maxTimeInTicks = TimeSpan.TicksPerSecond * 2;

        public static void Main(string[] args)
        {
            //using (StreamWriter writer = new StreamWriter(@"C:\Users\Kille\OneDrive\Documents\Projects\Halite\log.txt", false))
            {
                string name = args.Length > 0 ? args[0] : "Sharpie";

                Networking networking = new Networking();
                gameMap = networking.Initialize(name);
                playerId = gameMap.GetMyPlayerId();

                int totalGameTurns = 100 + (int)Math.Floor(Math.Sqrt(gameMap.GetWidth() * gameMap.GetHeight()));
                //writer.WriteLine("Total calculated turns: " + totalGameTurns);

                for (int i = 0; i < totalGameTurns; i++)
                {
                    PlayTurn();
                }
            }
        }

        // TODO fix starting docking, currently destroys most my ships from running into each other
        private static void PlayTurn()
        {
            DateTime turnStart = DateTime.UtcNow;

            List<Move> moveList = new List<Move>(500);
            gameMap.UpdateMap(Networking.ReadLineIntoMetadata());

            List<Planet> unownedPlanets = gameMap.GetAllPlanets().Values.Where(planet => !planet.IsOwned()).ToList();
            List<Planet> enemyPlanets = gameMap.GetAllPlanets().Values.Where(planet => planet.GetOwner() != playerId).ToList();
            List<Planet> ownedPlanets = gameMap.GetAllPlanets().Values.Where(planet => planet.IsOwned()).ToList();
            List<Planet> dockableOwnedPlanets = ownedPlanets.Where(planet => planet.GetDockedShips().Count < planet.GetDockingSpots()).ToList();

            List<Ship> undockedShips = GetAllUndockedShips().ToList();

            //foreach (Ship ship in GetAllUndockedShips())
            for (int i = 0; i < undockedShips.Count; i++)
            {
                Ship ship = undockedShips[i];

                if (unownedPlanets.Any())
                {
                    Planet closestPlanet = GetClosestPlanetToShip(ship, unownedPlanets);

                    if (ship.CanDock(closestPlanet))
                    {
                        if (ship.CanDock(closestPlanet))
                        {
                            moveList.Add(new DockMove(ship, closestPlanet));
                        }
                    }
                    else
                    {
                        ThrustMove newThrustMove = Navigation.NavigateShipToDock(gameMap, ship, closestPlanet, Constants.MAX_SPEED);
                        if (newThrustMove != null)
                        {
                            moveList.Add(newThrustMove);
                        }
                    }
                }
                else if (dockableOwnedPlanets.Any())
                {
                    Planet closestPlanet = GetClosestPlanetToShip(ship, dockableOwnedPlanets);

                    if (ship.CanDock(closestPlanet))
                    {
                        if (ship.CanDock(closestPlanet))
                        {
                            moveList.Add(new DockMove(ship, closestPlanet));
                        }
                    }
                    else
                    {
                        ThrustMove newThrustMove = Navigation.NavigateShipToDock(gameMap, ship, closestPlanet, Constants.MAX_SPEED);
                        if (newThrustMove != null)
                        {
                            moveList.Add(newThrustMove);
                        }
                    }
                }
                else if (enemyPlanets.Any())
                {
                    var planetAndDistance = GetClosestPlanetToShipWithDistance(ship, enemyPlanets);
                    var closestPlanet = planetAndDistance.Item1;
                    var distanceToPlanet = planetAndDistance.Item2;

                    ThrustMove newThrustMove = Navigation.NavigateShipTowardsTarget(gameMap, ship, closestPlanet, Constants.MAX_SPEED, distanceToPlanet > Constants.MAX_SPEED, 2, .1);
                    if (newThrustMove != null)
                    {
                        moveList.Add(newThrustMove);
                    }
                }

                double turnDelta = DateTime.UtcNow.Ticks - turnStart.Ticks;
                double shipDelta = (turnDelta / (i + 1)) * 2d;

                if (shipDelta > maxTimeInTicks - turnDelta)
                {
                    break;
                }
            }

            Networking.SendMoves(moveList);
        }

        private static IEnumerable<Ship> GetAllUndockedShips()
        {
            return gameMap.GetMyPlayer().GetShips().Values.Where(ship => ship.GetDockingStatus() == Ship.DockingStatus.Undocked);
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
    }
}
