﻿namespace AtomicTorch.CBND.CoreMod.Characters
{
    using System.Linq;
    using AtomicTorch.CBND.CoreMod.StaticObjects.Structures;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.CBND.GameApi.ServicesServer;
    using AtomicTorch.GameEngine.Common.Primitives;

    public static class ServerCharacterSpawnHelper
    {
        public const double SpawnMobNoPlayersRadius = 8;

        public const double SpawnNoPhysicsObjectsRadius = 1;

        public const double SpawnNoPlayerBuiltStructuresRadius = 8;

        public const double SpawnPlayerNoMobsRadius = 8;

        private static readonly IWorldServerService ServerWorldService = Api.Server.World;

        public static bool IsPositionValidForCharacterSpawn(
            Vector2D position,
            bool isPlayer)
        {
            var tile = ServerWorldService.GetTile(position.ToVector2Ushort());
            if (!IsValidTile(tile)
                || tile.EightNeighborTiles.Any(t => !IsValidTile(t)))
            {
                // cliff/slope or water nearby
                return false;
            }

            var physicsSpace = ServerWorldService.GetPhysicsSpace();
            using (var objectsNearby = physicsSpace.TestCircle(
                position,
                SpawnNoPlayerBuiltStructuresRadius,
                CollisionGroups.Default,
                sendDebugEvent: false))
            {
                if (objectsNearby.Any(
                    t => t.PhysicsBody.AssociatedWorldObject?.ProtoWorldObject is IProtoObjectStructure))
                {
                    // some structure nearby
                    return false;
                }
            }

            if (isPlayer)
            {
                // check if mobs nearby
                using (var mobsNearby = physicsSpace.TestCircle(
                    position,
                    SpawnPlayerNoMobsRadius,
                    CollisionGroups.Default,
                    sendDebugEvent: false))
                {
                    if (mobsNearby.Any(
                        t => t.PhysicsBody.AssociatedWorldObject is ICharacter otherCharacter
                             && otherCharacter.IsNpc))
                    {
                        // mobs nearby
                        return false;
                    }
                }
            }
            else
            {
                // check if mobs nearby
                using (var mobsNearby = physicsSpace.TestCircle(
                    position,
                    SpawnMobNoPlayersRadius,
                    CollisionGroups.Default,
                    sendDebugEvent: false))
                {
                    if (mobsNearby.Any(
                        t => t.PhysicsBody.AssociatedWorldObject is ICharacter otherCharacter
                             && !otherCharacter.IsNpc))
                    {
                        // players nearby
                        return false;
                    }
                }
            }

            // check if any physics object nearby
            using (var objectsNearby = physicsSpace.TestCircle(
                position,
                SpawnNoPhysicsObjectsRadius,
                CollisionGroups.Default,
                sendDebugEvent: false))
            {
                if (objectsNearby.Count > 0)
                {
                    // some physical object nearby
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidTile(Tile tile)
        {
            return !tile.IsCliffOrSlope
                   && tile.ProtoTile.Kind == TileKind.Solid;
        }
    }
}