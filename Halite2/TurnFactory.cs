using Halite2.hlt;

namespace Halite2
{
    public static class TurnFactory
    {
        public static Turn CreateTurn(GameMap gameMap, int turnCount)
        {
            Turn turn;

            if (turnCount < 10)
            {
                turn = new EarlyGameTurn(gameMap);
            }
            else if (turnCount >= 10 && turnCount < 50)
            {
                turn = new MidGameTurn(gameMap);
            }
            else
            {
                turn = new LateGameTurn(gameMap);
            }

            turn.RefreshGameData();

            return turn;
        }
    }
}
