﻿namespace AtomicTorch.CBND.CoreMod.Characters.Mobs
{
    using AtomicTorch.CBND.CoreMod.CharacterSkeletons;
    using AtomicTorch.CBND.CoreMod.Items.Food;
    using AtomicTorch.CBND.CoreMod.Items.Weapons.MobWeapons;
    using AtomicTorch.CBND.CoreMod.Skills;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Stats;
    using AtomicTorch.CBND.CoreMod.Systems.Droplists;

    public class MobCrab : ProtoCharacterMob
    {
        public override float CharacterWorldHeight => 0.6f;

        public override double MobKillExperienceMultiplier => 0.7;

        public override string Name => "Crab";

        public override ObjectSoundMaterial ObjectSoundMaterial => ObjectSoundMaterial.HardTissues;

        public override double StatDefaultHealthMax => 60;

        public override double StatMoveSpeed => 0.8;

        protected override void FillDefaultEffects(Effects effects)
        {
            base.FillDefaultEffects(effects);

            effects.AddValue(this, StatName.DefenseImpact, 0.4);
        }

        protected override void PrepareProtoCharacterMob(
            out ProtoCharacterSkeleton skeleton,
            ref double scale,
            DropItemsList lootDroplist)
        {
            skeleton = GetProtoEntity<SkeletonCrab>();

            // random loot
            lootDroplist
                .Add<ItemMeatRaw>(count: 1, probability: 1 / 2.0);

            // extra loot (with much lower chance)
            lootDroplist
                .Add<ItemMeatRaw>(count: 1, probability: 1 / 5.0, condition: SkillHunting.ServerRollExtraLoot);
        }

        protected override void ServerInitializeCharacterMob(ServerInitializeData data)
        {
            base.ServerInitializeCharacterMob(data);

            var weaponProto = GetProtoEntity<ItemWeaponCrabClaws>();
            data.PrivateState.WeaponState.SetWeaponProtoOnly(weaponProto);
            data.PublicState.SetCurrentWeaponProtoOnly(weaponProto);
        }

        protected override void ServerUpdateMob(ServerUpdateData data)
        {
            var character = data.GameObject;
            var currentStats = data.PublicState.CurrentStats;

            ServerCharacterAiHelper.ProcessAggressiveAi(
                character,
                isRetreating: currentStats.HealthCurrent < currentStats.HealthMax / 3,
                distanceRetreat: 7,
                distanceEnemyTooClose: 1,
                distanceEnemyTooFar: 4,
                out var movementDirection,
                out var rotationAngleRad);

            this.ServerSetMobInput(character, movementDirection, rotationAngleRad);
        }
    }
}