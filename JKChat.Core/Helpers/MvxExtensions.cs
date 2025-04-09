using System;

using Microsoft.Maui.ApplicationModel;

using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.Helpers {
	public static class MvxExtensions {
		public static MvxSubscriptionToken SubscribeOnMainThread2<TMessage>(this IMvxMessenger messenger, Action<TMessage> deliveryAction, MvxReference reference = MvxReference.Weak, string tag = null, bool isSticky = false) where TMessage : MvxMessage {
			return messenger.Subscribe<TMessage>(message => { MainThread.InvokeOnMainThreadAsync(() => deliveryAction(message)).Wait(); }, reference, tag, isSticky);
		}
	}
}