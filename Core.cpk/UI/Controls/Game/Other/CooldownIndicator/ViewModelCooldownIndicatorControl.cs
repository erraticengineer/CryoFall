﻿namespace AtomicTorch.CBND.CoreMod.UI.Controls.Game.Other.CooldownIndicator
{
    using System.Windows;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.Other.ClockProgressIndicator;

    public class ViewModelCooldownIndicatorControl : ViewModelClockProgressIndicator
    {
        private ClientComponentCooldownIndicator componentCooldownIndicatorUpdater;

        private bool isTurnedOn;

        private double timeRemainsSeconds;

        private double totalDurationSeconds;

        private Visibility visibility
#if GAME
			= Visibility.Hidden;
#else
            // in design-time - visible by default
            = Visibility.Visible;

#endif

        public ViewModelCooldownIndicatorControl() : base(
            isReversed: true,
            isAutoDisposeFields: true)
        {
        }

        public string TestText => "Test";

        public Visibility Visibility
        {
            get => this.visibility;
            set
            {
                if (value == this.visibility)
                {
                    return;
                }

                this.visibility = value;
                this.NotifyThisPropertyChanged();
            }
        }

        public void TurnOff()
        {
            if (IsDesignTime)
            {
                return;
            }

            if (this.isTurnedOn)
            {
                this.isTurnedOn = false;
                Logger.Info("Cooldown turn off!");

                this.componentCooldownIndicatorUpdater.SceneObject.Destroy();
                this.componentCooldownIndicatorUpdater = null;
            }

            this.Visibility = Visibility.Hidden;
        }

        public void TurnOn(double lengthSeconds)
        {
            if (IsDesignTime)
            {
                return;
            }

            if (lengthSeconds <= 0)
            {
                this.TurnOff();
                return;
            }

            Logger.Info("Cooldown started: " + lengthSeconds + " seconds");

            this.ResetLastProgressFraction();
            this.totalDurationSeconds = lengthSeconds;
            this.timeRemainsSeconds = lengthSeconds;

            if (!this.isTurnedOn)
            {
                this.isTurnedOn = true;
                this.Visibility = Visibility.Visible;

                this.componentCooldownIndicatorUpdater = Client.Scene.CreateSceneObject("Cooldown scene object")
                                                               .AddComponent<ClientComponentCooldownIndicator>();
                this.componentCooldownIndicatorUpdater.ViewModelToUpdate = this;
            }

            this.Update(0);
        }

        public void Update(double deltaTime)
        {
            this.timeRemainsSeconds -= deltaTime;

            var progressFraction = (this.totalDurationSeconds - this.timeRemainsSeconds) / this.totalDurationSeconds;
            if (progressFraction >= 1)
            {
                this.TurnOff();
                return;
            }

            this.ProgressFraction = progressFraction;
        }
    }
}