﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Signs
{
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems.Construction;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;
    using AtomicTorch.GameEngine.Common.Primitives;

    public class ObjectSignIron : ProtoObjectSign
    {
        public override string Description =>
            "More durable metal version of the sign. Still equally useful, but now more difficult for vandals to break.";

        public override string Name => "Iron sign";

        public override ObjectSoundMaterial ObjectSoundMaterial => ObjectSoundMaterial.Metal;

        public override double ObstacleBlockDamageCoef => 0.5;

        public override float StructurePointsMax => 1200;

        public override Vector2D SharedGetObjectCenterWorldOffset(IWorldObject worldObject)
        {
            return (0.5, 0.725);
        }

        protected override void ClientInitialize(ClientInitializeData data)
        {
            base.ClientInitialize(data);
            data.ClientState.RendererSignContent.PositionOffset = (0.1, 0.71);
            ClientFixSignDrawOffset(data);
        }

        protected override void ClientSetupRenderer(IComponentSpriteRenderer renderer)
        {
            base.ClientSetupRenderer(renderer);
            renderer.PositionOffset = (0.5, 0.2);
            renderer.DrawOrderOffsetY = 0.15;
        }

        protected override void PrepareConstructionConfig(
            ConstructionTileRequirements tileRequirements,
            ConstructionStageConfig build,
            ConstructionStageConfig repair,
            ConstructionUpgradeConfig upgrade,
            out ProtoStructureCategory category)
        {
            category = GetCategory<StructureCategoryOther>();

            build.StagesCount = 10;
            build.StageDurationSeconds = BuildDuration.Short;
            build.AddStageRequiredItem<ItemIngotIron>(count: 1);

            repair.StagesCount = 10;
            repair.StageDurationSeconds = BuildDuration.Short;
            repair.AddStageRequiredItem<ItemIngotIron>(count: 1);
        }

        protected override void SharedCreatePhysics(CreatePhysicsData data)
        {
            data.PhysicsBody
                .AddShapeRectangle((0.5, 0.2), (0.25, 0.4))
                .AddShapeRectangle((0.8, 1.0), (0.1, 0.4), CollisionGroups.HitboxMelee)
                .AddShapeRectangle((0.8, 1.0), (0.1, 0.4), CollisionGroups.HitboxRanged)
                .AddShapeRectangle((0.8, 1.0), (0.1, 0.4), CollisionGroups.ClickArea);
        }
    }
}