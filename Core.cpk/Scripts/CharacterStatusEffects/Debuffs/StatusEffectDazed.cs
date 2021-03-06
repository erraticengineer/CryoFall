﻿namespace AtomicTorch.CBND.CoreMod.CharacterStatusEffects.Debuffs
{
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects.Debuffs.Client;
    using AtomicTorch.CBND.CoreMod.CharacterStatusEffects.Neutral;
    using AtomicTorch.CBND.CoreMod.Stats;
    using AtomicTorch.CBND.CoreMod.Systems.Notifications;
    using AtomicTorch.CBND.GameApi.Data.Characters;

    public class StatusEffectDazed : ProtoStatusEffect
    {
        public const double MaxDuration = 4.0; // 4 seconds

        public const string NotificationCannotAttackWhileDazed = "Cannot attack while dazed.";

        public override string Description => "You are dazed! Give it a few moments to get your senses back.";

        public override double IntensityAutoDecreasePerSecondValue => 1 / MaxDuration;

        public override StatusEffectKind Kind => StatusEffectKind.Debuff;

        public override string Name => "Dazed";

        public static bool SharedIsCharacterDazed(ICharacter character, string clientMessageIfDazed)
        {
            if (!character.SharedHasStatusEffect<StatusEffectDazed>())
            {
                return false;
            }

            // cannot attack while dazed
            if (IsClient)
            {
                NotificationSystem.ClientShowNotification(
                    GetProtoEntity<StatusEffectDazed>().Name,
                    clientMessageIfDazed,
                    NotificationColor.Bad,
                    icon: GetProtoEntity<StatusEffectDazed>().Icon);
            }

            return true;
        }

        protected override void ClientDeinitialize(StatusEffectData data)
        {
            ClientComponentStatusEffectDazedManager.TargetIntensity = 0;
        }

        protected override void ClientUpdate(StatusEffectData data)
        {
            ClientComponentStatusEffectDazedManager.TargetIntensity = data.Intensity;
        }

        protected override void PrepareEffects(Effects effects)
        {
            // -50% move speed
            effects.AddPercent(this, StatName.MoveSpeed, -50);

            // cannot run
            effects.AddPercent(this, StatName.MoveSpeedRunMultiplier, -100);
        }

        protected override void ServerAddIntensity(StatusEffectData data, double intensityToAdd)
        {
            // does the character has drunk effect?
            if (data.Character.SharedHasStatusEffect<StatusEffectDrunk>())
            {
                // if so, then the character cannot be dazed
                return;
            }

            // otherwise add as normal
            base.ServerAddIntensity(data, intensityToAdd);
        }
    }
}