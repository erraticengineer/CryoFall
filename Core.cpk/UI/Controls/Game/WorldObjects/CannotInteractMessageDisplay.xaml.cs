﻿namespace AtomicTorch.CBND.CoreMod.UI.Controls.Game.WorldObjects
{
    using AtomicTorch.CBND.GameApi.Data.World;
    using AtomicTorch.CBND.GameApi.Scripting;
    using AtomicTorch.CBND.GameApi.ServicesClient.Components;
    using AtomicTorch.GameEngine.Common.Client.MonoGame.UI;

    public partial class CannotInteractMessageDisplay : BaseUserControl
    {
        private const double TimeoutSeconds = 1.5;

        private static CannotInteractMessageDisplay lastControl;

        private IComponentAttachedControl componentAttachedControl;

        public static void Hide()
        {
            lastControl?.componentAttachedControl.Destroy();
            lastControl = null;
        }

        public static void ShowOn(IWorldObject worldObject, string message)
        {
            Hide();

            var staticWorldObject = worldObject as IStaticWorldObject;
            var protoStaticWorldObject = staticWorldObject?.ProtoStaticWorldObject;

            var positionOffset = protoStaticWorldObject != null
                                     ? protoStaticWorldObject.SharedGetObjectCenterWorldOffset(staticWorldObject)
                                     : (0, 0);

            positionOffset += (0, 1.025);

            lastControl = new CannotInteractMessageDisplay();
            lastControl.Setup(message);

            lastControl.componentAttachedControl = Api.Client.UI.AttachControl(
                worldObject,
                lastControl,
                positionOffset: positionOffset,
                isFocusable: false);

            lastControl.componentAttachedControl.Destroy(delay: TimeoutSeconds);
        }

        public void Setup(string message)
        {
            this.DataContext = message;
        }

        protected override void InitControl()
        {
        }

        protected override void OnUnloaded()
        {
            if (!ReferenceEquals(lastControl, this))
            {
                return;
            }

            lastControl = null;
            // reset interaction tooltip (so it will be displayed again)
            InteractionTooltip.Hide();
        }
    }
};