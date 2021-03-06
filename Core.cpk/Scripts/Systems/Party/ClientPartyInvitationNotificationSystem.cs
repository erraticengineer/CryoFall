﻿namespace AtomicTorch.CBND.CoreMod.Systems.Party
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using AtomicTorch.CBND.CoreMod.Systems.Chat;
    using AtomicTorch.CBND.CoreMod.Systems.Notifications;
    using AtomicTorch.CBND.CoreMod.UI;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Core;
    using AtomicTorch.CBND.CoreMod.UI.Controls.Game.HUD.Notifications;
    using AtomicTorch.CBND.GameApi.Resources;

    public class ClientPartyInvitationNotificationSystem
        : ProtoSystem<ClientPartyInvitationNotificationSystem>
    {
        public const string InvitationMessageFormat
            = "[b]{0}[/b] invited you to join their party.";

        public const string InvitationMessageYouWillLeaveYourParty
            = "Please note: you will [b]leave[/b] your current party if you accept this invitation.";

        public const string PartyInvitationTitle
            = "Party invitation";

        private static readonly Dictionary<string, WeakReference<HUDNotificationControl>>
            NotificationsFromInviteeDictionary
                = new Dictionary<string, WeakReference<HUDNotificationControl>>(StringComparer.Ordinal);

        public override string Name => "Party invitations (client) system";

        protected override void PrepareSystem()
        {
            if (IsClient)
            {
                PartySystem.ClientCurrentInvitationsFromCharacters.CollectionChanged
                    += this.InvitationsCollectionChangedHandler;
            }
        }

        private static void ShowInvitationDialog(string inviterName)
        {
            var text = string.Format(InvitationMessageFormat, inviterName);
            if (PartySystem.ClientGetCurrentPartyMembers().Count > 1)
            {
                // ReSharper disable once CanExtractXamlLocalizableStringCSharp
                text += "[br]" + InvitationMessageYouWillLeaveYourParty;
            }

            DialogWindow.ShowDialog(
                title: PartyInvitationTitle,
                text: text,
                okText: CoreStrings.Button_Accept,
                okAction: () => PartySystem.ClientInvitationAccept(inviterName),
                cancelText: CoreStrings.Button_Deny,
                cancelAction: () => PartySystem.ClientInvitationDecline(inviterName),
                focusOnCancelButton: true,
                closeByEscapeKey: false);
        }

        private void InvitationsCollectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            string inviterName;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    inviterName = (string)e.NewItems[0];
                    ShowNotification(inviterName);
                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    inviterName = (string)e.OldItems[0];
                    if (NotificationsFromInviteeDictionary.TryGetValue(inviterName, out var weakReference)
                        && weakReference.TryGetTarget(out var control))
                    {
                        control.Hide(quick: true);
                    }

                    NotificationsFromInviteeDictionary.Remove(inviterName);
                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    // hide all current notifications
                    foreach (var notificationControl in NotificationsFromInviteeDictionary)
                    {
                        var weakReference = notificationControl.Value;
                        if (weakReference.TryGetTarget(out var control))
                        {
                            control.Hide(quick: true);
                        }
                    }

                    NotificationsFromInviteeDictionary.Clear();

                    // display new notifications
                    foreach (var name in PartySystem.ClientCurrentInvitationsFromCharacters)
                    {
                        ShowNotification(name);
                    }

                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }

            void ShowNotification(string name)
            {
                if (ClientChatBlockList.IsBlocked(name))
                {
                    // don't display invitations from blocked players
                    return;
                }

                var control = NotificationSystem.ClientShowNotification(
                    PartyInvitationTitle,
                    string.Format(InvitationMessageFormat, name),
                    onClick: () => ShowInvitationDialog(name),
                    autoHide: false,
                    icon: new TextureResource("Icons/IconPartyInvitation"));

                NotificationsFromInviteeDictionary.Add(
                    name,
                    new WeakReference<HUDNotificationControl>(control));
            }
        }
    }
}