using System;

namespace Halite2.hlt
{
    public static class Constants
    {

        ////////////////////////////////////////////////////////////////////////
        // Implementation-independent language-agnostic constants

        /** Games will not have more players than this */
        public const int MAX_PLAYERS = 4;

        /** Max number of units of distance a ship can travel in a turn */
        public const int MAX_SPEED = 7;

        /** Radius of a ship */
        public const double SHIP_RADIUS = 0.5;

        /** Starting health of ship, also its max */
        public const int MAX_SHIP_HEALTH = 255;

        /** Starting health of ship, also its max */
        public const int BASE_SHIP_HEALTH = 255;

        /** Weapon cooldown period */
        public const int WEAPON_COOLDOWN = 1;

        /** Weapon damage radius */
        public const double WEAPON_RADIUS = 5.0;

        /** Weapon damage */
        public const int WEAPON_DAMAGE = 64;

        /** Radius in which explosions affect other entities */
        public const double EXPLOSION_RADIUS = 10.0;

        /** Distance from the edge of the planet at which ships can try to dock */
        public const double DOCK_RADIUS = 4.0;

        /** Number of turns it takes to dock a ship */
        public const int DOCK_TURNS = 5;

        /** Number of production units per turn contributed by each docked ship */
        public const int BASE_PRODUCTIVITY = 6;

        /** Distance from the planets edge at which new ships are created */
        public const double SPAWN_RADIUS = 2.0;

        ////////////////////////////////////////////////////////////////////////
        // Implementation-specific constants

        public const double FORECAST_FUDGE_FACTOR = SHIP_RADIUS + 0.1;
        public const int MAX_NAVIGATION_CORRECTIONS = 90;
        public const double NAVIGATION_CORRECTION_STEP = Math.PI / 180.0;

        /**
         * Used in Position.getClosestPoint()
         * Minimum distance specified from the object's outer radius.
         */
        public const int MIN_DISTANCE_FOR_CLOSEST_POINT = 3;
    }
}
