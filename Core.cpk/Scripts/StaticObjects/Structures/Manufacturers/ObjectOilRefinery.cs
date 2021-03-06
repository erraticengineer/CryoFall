﻿namespace AtomicTorch.CBND.CoreMod.StaticObjects.Structures.Manufacturers
{
    using System.Windows.Media;
    using AtomicTorch.CBND.CoreMod.ClientComponents.Rendering.Lighting;
    using AtomicTorch.CBND.CoreMod.Items.Generic;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems.Construction;
    using AtomicTorch.CBND.CoreMod.Systems.Physics;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;

    public class ObjectOilRefinery : ProtoObjectOilRefinery
    {
        private readonly TextureAtlasResource textureAtlasOilPumpActive;

        public ObjectOilRefinery()
        {
            this.textureAtlasOilPumpActive = new TextureAtlasResource(
                this.GenerateTexturePath() + "Active",
                columns: 2,
                rows: 4,
                isTransparent: false);
        }

        public override byte ContainerFuelSlotsCount => 1;

        public override byte ContainerInputSlotsCount => 1;

        public override byte ContainerOutputSlotsCount => 1;

        public override string Description =>
            "Refines raw petroleum oil into more useful components, such as fuel and mineral oil.";

        public override float LiquidCapacityGasoline => 100;

        public override float LiquidCapacityMineralOil => 100;

        public override float LiquidCapacityRawPetroleum => 100;

        public override float LiquidGasolineProductionPerSecond => 0.2f;

        public override float LiquidMineralOilProductionPerSecond => 0.2f;

        public override float LiquidRawPetroleumConsumptionPerSecond => 0.2f;

        public override string Name => "Oil refinery";

        public override ObjectSoundMaterial ObjectSoundMaterial => ObjectSoundMaterial.Metal;

        public override double ObstacleBlockDamageCoef => 1.0;

        public override float StructurePointsMax => 10000;

        protected override void ClientInitialize(ClientInitializeData data)
        {
            base.ClientInitialize(data);
            var worldObject = data.GameObject;

            // setup light source
            var lightSource = ClientLighting.CreateLightSourceSpot(
                Client.Scene.GetSceneObject(worldObject),
                color: Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF),
                size: 1,
                spritePivotPoint: (0.5, 0.5),
                positionOffset: (1.47, 0.78));

            var animatedSpritePositionOffset = (277 / (double)ScriptingConstants.TileSizeRealPixels,
                                                171 / (double)ScriptingConstants.TileSizeRealPixels);
            this.ClientSetupManufacturerActiveAnimation(
                worldObject,
                data.SyncPublicState,
                this.textureAtlasOilPumpActive,
                animatedSpritePositionOffset,
                frameDurationSeconds: 0.3,
                autoInverseAnimation: true,
                onRefresh: isActive => { lightSource.IsEnabled = isActive; });
        }

        protected override void ClientSetupRenderer(IComponentSpriteRenderer renderer)
        {
            base.ClientSetupRenderer(renderer);
            renderer.DrawOrderOffsetY = 1.8;
        }

        protected override void CreateLayout(StaticObjectLayout layout)
        {
            layout.Setup("###",
                         "###",
                         "###");
        }

        protected override void PrepareConstructionConfig(
            ConstructionTileRequirements tileRequirements,
            ConstructionStageConfig build,
            ConstructionStageConfig repair,
            ConstructionUpgradeConfig upgrade,
            out ProtoStructureCategory category)
        {
            category = GetCategory<StructureCategoryIndustry>();

            build.StagesCount = 10;
            build.StageDurationSeconds = BuildDuration.Short;
            build.AddStageRequiredItem<ItemIngotSteel>(count: 25);
            build.AddStageRequiredItem<ItemIngotCopper>(count: 25);
            build.AddStageRequiredItem<ItemCement>(count: 50);

            repair.StagesCount = 10;
            repair.StageDurationSeconds = BuildDuration.Short;
            repair.AddStageRequiredItem<ItemIngotSteel>(count: 5);
            repair.AddStageRequiredItem<ItemIngotCopper>(count: 2);
        }

        protected override void PrepareDefense(DefenseDescription defense)
        {
            defense.Set(ObjectDefensePresets.Tier1);
        }

        protected override void SharedCreatePhysics(
            CreatePhysicsData data)
        {
            data.PhysicsBody
                .AddShapeRectangle((3, 2),   (0, 0))
                .AddShapeRectangle((3, 2.2), (0, 0), CollisionGroups.HitboxMelee)
                .AddShapeRectangle((3, 2.4), (0, 0), CollisionGroups.HitboxRanged)
                .AddShapeRectangle((3, 2.6), (0, 0), CollisionGroups.ClickArea);
        }
    }
}