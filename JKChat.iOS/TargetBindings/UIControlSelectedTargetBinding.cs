using System;

using MvvmCross.Binding;
using MvvmCross.Binding.Bindings.Target;
using MvvmCross.WeakSubscription;

using UIKit;

namespace JKChat.iOS.TargetBindings {
	public class UIControlSelectedTargetBinding : MvxTargetBinding<UIControl, bool> {
		private IDisposable subscription;

		public UIControlSelectedTargetBinding(UIControl target) : base(target) {}

		protected override void SetValue(bool value) {
			Target.Selected = value;
		}

		public override void SubscribeToEvents() {
			var uiControl = Target;
			if (uiControl == null) {
				MvxBindingLog.Error("Error - Control is null in UIControlSelectedTargetBinding");
				return;
			}

			subscription = uiControl.WeakSubscribe(nameof(uiControl.TouchUpInside), HandleValueChanged);
		}

		public override MvxBindingMode DefaultMode => MvxBindingMode.TwoWay;

		protected override void Dispose(bool isDisposing) {
			base.Dispose(isDisposing);
			if (!isDisposing) return;

			subscription?.Dispose();
			subscription = null;
		}

		private void HandleValueChanged(object sender, EventArgs ev) {
			Target.Selected = !Target.Selected;
			FireValueChanged(Target.Selected);
		}
	}
}