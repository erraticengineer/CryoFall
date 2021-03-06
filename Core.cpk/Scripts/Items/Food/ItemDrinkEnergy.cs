﻿namespace AtomicTorch.CBND.CoreMod.Items.Food
{
    using System;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects.Buffs;
    using AtomicTorch.CBND.CoreMod.SoundPresets;

    public class ItemDrinkEnergy : ProtoItemFood
    {
        public override string Description =>
            "Energy drink popular throughout the galaxy. Gives you serious energy boost.";

        public override TimeSpan FreshnessDuration => ExpirationDuration.Preserved;

        public override string ItemUseCaption => ItemUseCaptions.Drink;

        public override string Name => "Energy drink";

        public override ushort OrganicValue => 0;

        public override float StaminaRestore => 200;

        public override float WaterRestore => 15;

        protected override ReadOnlySoundPreset<ItemSound> PrepareSoundPresetItem()
        {
            return ItemsSoundPresets.ItemFoodDrinkCan;
        }

        protected override void ServerOnEat(ItemEatData data)
        {
            data.Character.ServerAddStatusEffect<StatusEffectEnergyRush>(intensity: 0.3); // 3 minutes
            base.ServerOnEat(data);
        }
    }
}