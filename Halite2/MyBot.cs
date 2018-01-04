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
        private static int turnCount = 0;

        public static void Main(string[] args)
        {
            //Debugger.Break();
            Log.IsEnabled = false;

            try
            {
                Networking networking = new Networking();
                gameMap = networking.Initialize("MrBot");

                //try
                {
                    while (true)
                    {
                        PlayTurn();
                    }
                }
                //catch (FormatException)
                //{
                //    //Game over
                //}
            }
            catch (Exception e)
            {
                Log.LogMessage(e.ToString());
                throw;
            }
        }

        private static void PlayTurn()
        {
            turnCount++;
            Log.LogMessage($"Starting turn: {turnCount}");

            Turn turn = TurnFactory.CreateTurn(gameMap, turnCount);
            Networking.SendMoves(turn.Play());
        }
    }
}
