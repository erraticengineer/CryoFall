﻿namespace AtomicTorch.CBND.CoreMod.Items.Weapons
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Controls;
    using AtomicTorch.CBND.CoreMod.Characters;
    using AtomicTorch.CBND.CoreMod.Characters.Player;
    using AtomicTorch.CBND.CoreMod.ClientComponents.Input;
    using AtomicTorch.CBND.CoreMod.Helpers.Client;
    using AtomicTorch.CBND.CoreMod.Items.Ammo;
    using AtomicTorch.CBND.CoreMod.Skills;
    using AtomicTorch.CBND.CoreMod.SoundPresets;
    using AtomicTorch.CBND.CoreMod.Systems.ItemDurability;
    using AtomicTorch.CBND.CoreMod.Systems.Weapons;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.Items.Controls.HotbarOverlays;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.Items.Controls.SlotOverlays;
    using AtomicTorch.CBND.GameApi.Data.Characters;
    using AtomicTorch.CBND.GameApi.Data.Items;
    using AtomicTorch.CBND.GameApi.Data.State;
    using AtomicTorch.CBND.GameApi.Data.Weapons;
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Resources;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.CBND.GameApi.Scripting.ClientComponents;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;
    using AtomicTorch.GameEngine.Common.Helpers;

    public abstract class ProtoItemWeapon
        <TPrivateState,
         TPublicState,
         TClientState>
        : ProtoItem
          <TPrivateState,
              TPublicState,
              TClientState>,
          IProtoItemWeapon
        where TPrivateState : WeaponPrivateState, new()
        where TPublicState : BasePublicState, new()
        where TClientState : BaseClientState, new()
    {
        private readonly Lazy<ProtoSkillWeapons> lazyWeaponSkillPrototype;

        private TextureResource cachedWeaponTextureResource;

        private ClientInputContext helperInputListener;

        protected ProtoItemWeapon()
        {
            this.lazyWeaponSkillPrototype = new Lazy<ProtoSkillWeapons>(
                () => this.WeaponSkill);
        }

        public abstract ushort AmmoCapacity { get; }

        public virtual ushort AmmoConsumptionPerShot => this.AmmoCapacity > 0
                                                            ? (ushort)1
                                                            : (ushort)0;

        public abstract double AmmoReloadDuration { get; }

        public virtual double ArmorPiercingMultiplier => 1;

        public abstract string CharacterAnimationAimingName { get; }

        public IReadOnlyCollection<IProtoItemAmmo> CompatibleAmmoProtos { get; private set; }

        public virtual double DamageApplyDelay => 0;

        public virtual double DamageMultiplier => 1;

        public virtual ushort DurabilityDecreasePerAction => 1;

        public abstract ushort DurabilityMax { get; }

        public abstract double FireAnimationDuration { get; }

        public abstract double FireInterval { get; }

        public override double GroundIconScale => 1.5;

        public override ITextureResource Icon => new TextureResource("Items/Weapons/" + this.GetType().Name);

        public virtual bool IsLoopedAttackAnimation => (this.FireInterval / this.FireAnimationDuration) < 1.5;

        public sealed override ushort MaxItemsPerStack => 1;

        public DamageDescription OverrideDamageDescription { get; private set; }

        public virtual double RangeMultipier => 1;

        public virtual (float min, float max) SoundPresetWeaponDistance 
            => (SoundConstants.AudioListenerMinDistance, SoundConstants.AudioListenerMaxDistance);

        /// <inheritdoc />
        public ReadOnlySoundPreset<ObjectSoundMaterial> SoundPresetHit { get; private set; }

        public ReadOnlySoundPreset<WeaponSound> SoundPresetWeapon { get; private set; }

        public virtual double SpecialEffectProbability => 0;

        public virtual string WeaponAttachmentName => "WeaponRifle";

        public ProtoSkillWeapons WeaponSkillProto => this.lazyWeaponSkillPrototype.Value;

        ITextureResource IProtoItemWeapon.WeaponTextureResource => this.cachedWeaponTextureResource;

        protected abstract ProtoSkillWeapons WeaponSkill { get; }

        protected virtual TextureResource WeaponTextureResource => new TextureResource(
            "Characters/Weapons/" + this.GetType().Name,
            isProvidesMagentaPixelPosition: true);

        public Control ClientCreateHotbarOverlayControl(IItem item)
        {
            if (this.CompatibleAmmoProtos.Count == 0)
            {
                // no ammo display required
                return null;
            }

            return new HotbarItemWeaponOverlayControl(item);
        }

        public void ClientCreateItemSlotOverlayControls(IItem item, List<Control> controls)
        {
            if (this.DurabilityMax > 0)
            {
                controls.Add(ItemSlotDurabilityOverlayControl.Create(item));
            }
        }

        public virtual void ClientSetupSkeleton(
            IItem item,
            ICharacter character,
            IComponentSkeleton skeletonRenderer,
            List<IClientComponent> skeletonComponents)
        {
            ClientSkeletonItemInHandHelper.Setup(
                skeletonRenderer,
                this.WeaponAttachmentName,
                this.cachedWeaponTextureResource);
        }

        public virtual string GetCharacterAnimationNameFire(ICharacter character)
        {
            // no animation by default
            return null;
        }

        public virtual void ServerOnDamageApplied(
            IItem weapon,
            ICharacter byCharacter,
            IWorldObject damagedObject,
            double damage)
        {
            if (!(damagedObject is ICharacter damagedCharacter))
            {
                return;
            }

            var protoItemAmmo = weapon != null
                                    ? GetPrivateState(weapon).CurrentProtoItemAmmo
                                    : null;

            protoItemAmmo?.ServerOnCharacterHit(damagedCharacter, damage);

            if (damage > 0)
            {
                this.ServerTryToApplySpecialEffect(weapon, byCharacter, damage, damagedCharacter);
            }
        }

        public virtual void ServerOnItemBrokeAndDestroyed(IItem item, IItemsContainer container, byte slotId)
        {
            if (container == null
                || container.IsDestroyed)
            {
                return;
            }

            // try to unload the ammo
            var privateState = GetPrivateState(item);
            var ammoCount = privateState.AmmoCount;
            if (ammoCount == 0)
            {
                return;
            }

            privateState.AmmoCount = 0;

            // try spawn into the destroyed item slot
            var result = Server.Items.CreateItem(
                container: container,
                protoItem: privateState.CurrentProtoItemAmmo,
                count: ammoCount);
            if (result.IsEverythingCreated
                || container.OwnerAsCharacter == null)
            {
                // spawned successfully or the owner is not a character
                return;
            }

            // spawned unsuccessfully - try to spawn in any container for character
            result.Rollback();
            Server.Items.CreateItem(
                container.OwnerAsCharacter,
                protoItem: privateState.CurrentProtoItemAmmo,
                count: ammoCount);
        }

        public virtual void ServerOnItemDamaged(IItem item, double damageApplied)
        {
            ItemDurabilitySystem.ServerModifyDurability(item, delta: -(int)damageApplied);
        }

        public void ServerOnShot(
            ICharacter character,
            IItem weaponItem,
            IProtoItemWeapon protoWeapon,
            List<WeaponHitData> hitObjects)
        {
            if (weaponItem != null)
            {
                ServerItemUseObserver.NotifyItemUsed(character, weaponItem);
            }

            // try reduce weapon durability
            // for ranged weapon - when shot
            // for melee weapon - when at least one object was hit
            var characterSkills = character.SharedGetSkills();
            if (characterSkills == null)
            {
                // not a player character - don't degrade the weapon
                return;
            }

            var shouldDegrade = true;
            if (this.WeaponSkillProto != null)
            {
                // get degradation probability
                var probability = character.SharedGetFinalStatMultiplier(
                    this.WeaponSkillProto.StatNameDegrationRateMultiplier);
                probability = MathHelper.Clamp(probability, 0, 1);
                if (!RandomHelper.RollWithProbability(probability))
                {
                    shouldDegrade = false;
                }
            }

            if (shouldDegrade)
            {
                this.ServerOnDegradeWeapon(character, weaponItem, protoWeapon, hitObjects);
            }
        }

        public virtual bool SharedCanFire(ICharacter character, WeaponState weaponState)
        {
            if (this.AmmoCapacity == 0)
            {
                // not uses ammo
                return true;
            }

            // weapon uses ammo
            var itemWeapon = weaponState.ActiveItemWeapon;
            if (itemWeapon == null)
            {
                return false;
            }

            if (itemWeapon.ProtoItem != this)
            {
                Logger.Error($"Weapon proto mismatch: CanFire check for weapon {itemWeapon} executed for proto {this}",
                             character);
                return false;
            }

            var weaponPrivateState = GetPrivateState(itemWeapon);
            if (weaponPrivateState.AmmoCount >= this.AmmoConsumptionPerShot)
            {
                return true;
            }

            // not enough ammo
            if (IsClient && weaponState.SharedGetInputIsFiring())
            {
                // try auto-reloading
                WeaponAmmoSystem.ClientTryReloadOrSwitchAmmoType(isSwitchAmmoType: false);
            }

            return false;
        }

        public virtual bool SharedOnFire(ICharacter character, WeaponState weaponState)
        {
            // consume ammo
            if (this.AmmoConsumptionPerShot == 0)
            {
                // not using ammo
                return true;
            }

            var privateState = GetPrivateState(weaponState.ActiveItemWeapon);
            if (privateState.AmmoCount < this.AmmoConsumptionPerShot)
            {
                // not enough ammo
                return false;
            }

            privateState.AmmoCount -= this.AmmoConsumptionPerShot;
            return true;
        }

        protected static ICollection<TAmmo> GetAmmoOfType<TAmmo>()
            where TAmmo : class, IProtoItemAmmo
        {
            return Api.FindProtoEntities<TAmmo>();
        }

        protected static TProtoSkillWeapons GetSkill<TProtoSkillWeapons>()
            where TProtoSkillWeapons : ProtoSkillWeapons, new()
        {
            return Api.GetProtoEntity<TProtoSkillWeapons>();
        }

        protected override void ClientItemHotbarSelectionChanged(ClientHotbarItemData data)
        {
            if (data.IsSelected)
            {
                // ReSharper disable once CanExtractXamlLocalizableStringCSharp
                this.helperInputListener = ClientInputContext
                                           .Start("Current weapon")
                                           .HandleButtonDown(
                                               GameButton.ItemReload,
                                               () => WeaponAmmoSystem.ClientTryReloadOrSwitchAmmoType(
                                                   isSwitchAmmoType: false))
                                           .HandleButtonDown(
                                               GameButton.ItemSwitchMode,
                                               () => WeaponAmmoSystem.ClientTryReloadOrSwitchAmmoType(
                                                   isSwitchAmmoType: true));
            }
            else
            {
                this.helperInputListener?.Stop();
                this.helperInputListener = null;
            }
        }

        protected override bool ClientItemUseFinish(ClientItemData data)
        {
            WeaponSystem.ClientChangeWeaponFiringMode(isFiring: false);
            // we don't want to play any "item use" sound
            return false;
        }

        protected override void ClientItemUseStart(ClientItemData data)
        {
            WeaponSystem.ClientChangeWeaponFiringMode(isFiring: true);
        }

        protected override void PrepareProtoItem()
        {
            base.PrepareProtoItem();

            if (this.FireAnimationDuration > this.FireInterval)
            {
                throw new Exception(
                    $"{nameof(this.FireAnimationDuration)} is bigger than {nameof(this.FireInterval)}");
            }

            this.cachedWeaponTextureResource = this.WeaponTextureResource;

            DamageDescription overrideDamageDescription = null;
            this.PrepareProtoWeapon(
                out var compatibleAmmoProtos,
                ref overrideDamageDescription);
            this.SoundPresetWeapon = this.PrepareSoundPresetWeapon();
            this.SoundPresetHit = this.PrepareSoundPresetHit();

            this.CompatibleAmmoProtos =
                (compatibleAmmoProtos?.Distinct() ?? Enumerable.Empty<IProtoItemAmmo>())
                .ToList();

            if (this.CompatibleAmmoProtos.Count == 0
                && overrideDamageDescription == null)
            {
                throw new Exception(
                    $"The weapon {this} doesn't have compatible ammo and overrideDamageDescription is null.");
            }

            this.OverrideDamageDescription = overrideDamageDescription;

            if (this.DamageApplyDelay >= this.FireInterval
                && this.DamageApplyDelay > 0)
            {
                throw new Exception("DamageApplyDelaySeconds must be lower than FireIntervalSeconds for " + this);
            }

            if (this.SpecialEffectProbability < 0
                || this.SpecialEffectProbability > 1)
            {
                throw new Exception(
                    $"The {nameof(this.SpecialEffectProbability)} property returning an incorrect value: the value must be in [0;1] range.");
            }

            if (this.AmmoCapacity > 0)
            {
                if (this.AmmoCapacity < this.AmmoConsumptionPerShot)
                {
                    throw new Exception(
                        $"{nameof(this.AmmoCapacity)} is less than {nameof(this.AmmoConsumptionPerShot)}");
                }

                if (this.AmmoCapacity % this.AmmoConsumptionPerShot > 0)
                {
                    throw new Exception(nameof(this.AmmoCapacity)
                                        + " cannot be divided without remainder on "
                                        + nameof(this.AmmoConsumptionPerShot)
                                        + " please adjust one or both of the properties accordingly");
                }
            }
            else if (this.AmmoConsumptionPerShot > 0)
            {
                throw new Exception("This weapon doesn't use ammo but overrides property "
                                    + nameof(this.AmmoConsumptionPerShot));
            }
        }

        protected abstract void PrepareProtoWeapon(
            out IEnumerable<IProtoItemAmmo> compatibleAmmoProtos,
            ref DamageDescription overrideDamageDescription);

        protected abstract ReadOnlySoundPreset<ObjectSoundMaterial> PrepareSoundPresetHit();

        protected abstract ReadOnlySoundPreset<WeaponSound> PrepareSoundPresetWeapon();

        protected override void ServerInitialize(ServerInitializeData data)
        {
            base.ServerInitialize(data);
            ItemDurabilitySystem.ServerInitializeItem(data.PrivateState, data.IsFirstTimeInit);
        }

        protected abstract void ServerOnDegradeWeapon(
            ICharacter character,
            IItem weaponItem,
            IProtoItemWeapon protoWeapon,
            List<WeaponHitData> hitObjects);

        protected virtual void ServerOnSpecialEffect(ICharacter damagedCharacter, double damage)
        {
        }

        private void ServerTryToApplySpecialEffect(
            IItem weapon,
            ICharacter byCharacter,
            double damage,
            ICharacter damagedCharacter)
        {
            IProtoItemAmmo protoItemAmmo;
            var probability = this.SpecialEffectProbability;
            if (this.WeaponSkillProto != null)
            {
                var statNameSpecialEffectChance = this.WeaponSkillProto.StatNameSpecialEffectChanceMultiplier;
                probability *= byCharacter.SharedGetFinalStatMultiplier(statNameSpecialEffectChance);
            }

            //Logger.WriteDev($"Special effect probability: {specialEffectProbability} {this}");

            if (probability >= 1
                || probability > 0 && RandomHelper.RollWithProbability(probability))
            {
                // the special effect has been rolled - try to apply it
                if (weapon != null)
                {
                    protoItemAmmo = GetPrivateState(weapon).CurrentProtoItemAmmo;

                    if (protoItemAmmo == null
                        || !protoItemAmmo.IsSuppressWeaponSpecialEffect)
                    {
                        // call special effect for the weapon proto
                        this.ServerOnSpecialEffect(damagedCharacter, damage);
                    }
                }
                else // no weapon item (only the weapon item prototype is available - this)
                {
                    // call special effect for the weapon proto
                    this.ServerOnSpecialEffect(damagedCharacter, damage);
                }
            }
        }
    }
}