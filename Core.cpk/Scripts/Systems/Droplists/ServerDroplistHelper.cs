﻿namespace AtomicTorch.CBND.CoreMod.Systems.Droplists
{
    using AtomicTorch.CBND.CoreMod.StaticObjects;
    using AtomicTorch.CBND.CoreMod.Systems.Notifications;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.Items;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.CBND.GameApi.ServicesServer;
    using AtomicTorch.GameEngine.Common.Primitives;

    public static class ServerDroplistHelper
    {
        public static CreateItemResult TryDropToCharacter(
            IReadOnlyDropItemsList dropItemsList,
            ICharacter character,
            bool sendNoFreeSpaceNotification,
            double probabilityMultiplier,
            DropItemContext context)
        {
            if (character == null)
            {
                return new CreateItemResult() { IsEverythingCreated = false };
            }

            var itemsService = Api.Server.Items;

            var result = dropItemsList.Execute(
                (protoItem, count) => itemsService.CreateItem(character, protoItem, count),
                context,
                probabilityMultiplier);

            if (sendNoFreeSpaceNotification
                && !result.IsEverythingCreated)
            {
                NotificationSystem.ServerSendNotificationNoSpaceInInventory(character);
            }

            return result;
        }

        public static CreateItemResult TryDropToCharacterOrGround(
            IReadOnlyDropItemsList dropItemsList,
            ICharacter toCharacter,
            Vector2Ushort tilePosition,
            bool sendNotificationWhenDropToGround,
            double probabilityMultiplier,
            DropItemContext context)
        {
            CreateItemResult result;
            if (toCharacter != null)
            {
                const bool sendNoFreeSpaceNotification = false;
                result = TryDropToCharacter(
                    dropItemsList,
                    toCharacter,
                    sendNoFreeSpaceNotification,
                    probabilityMultiplier,
                    context);
                if (result.IsEverythingCreated)
                {
                    return result;
                }

                // cannot add to character - rollback and drop on ground instead
                result.Rollback();
            }

            result = TryDropToGround(dropItemsList, tilePosition, probabilityMultiplier, context);
            if (result.TotalCreatedCount > 0)
            {
                // notify player that there were not enough space in inventory so the items were dropped to the ground
                NotificationSystem.ServerSendNotificationNoSpaceInInventoryItemsDroppedToGround(toCharacter);
            }

            return result;
        }

        public static CreateItemResult TryDropToContainer(
            IReadOnlyDropItemsList dropItemsList,
            IItemsContainer toContainer,
            double probabilityMultiplier,
            DropItemContext context)
        {
            var itemsService = Api.Server.Items;

            var result = dropItemsList.Execute(
                (protoItem, count) => itemsService.CreateItem(protoItem, toContainer, count),
                context,
                probabilityMultiplier);

            return result;
        }

        public static CreateItemResult TryDropToGround(
            IReadOnlyDropItemsList dropItemsList,
            Vector2Ushort tilePosition,
            double probabilityMultiplier,
            DropItemContext context)
        {
            // obtain the ground container to drop the items into
            var tile = Api.Server.World.GetTile(tilePosition);
            var groundContainer = ObjectGroundItemsContainer
                .ServerTryGetOrCreateGroundContainerAtTileOrNeighbors(tile);

            if (groundContainer == null)
            {
                // cannot drop because there are no free space available on the ground
                return new CreateItemResult() { IsEverythingCreated = false };
            }

            var result = dropItemsList.TryDropToContainer(groundContainer, context, probabilityMultiplier);

            if (result.TotalCreatedCount == 0
                && groundContainer.OccupiedSlotsCount == 0)
            {
                // nothing is spawned, the ground container should be destroyed
                Api.Server.World.DestroyObject(groundContainer.OwnerAsStaticObject);
            }

            return result;
        }
    }
}