using System.Diagnostics.CodeAnalysis;

using Android.Text;
using Android.Widget;

using MvvmCross.Binding;
using MvvmCross.Platforms.Android.Binding.Target;

namespace JKChat.Android.TargetBindings {
	public class TextSwitcherTextFormattedTargetBinding : MvxAndroidTargetBinding<TextSwitcher, ISpanned> {
		public TextSwitcherTextFormattedTargetBinding([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents)] TextSwitcher target)
			: base(target) {}

		protected override void SetValueImpl(TextSwitcher target, ISpanned value) {
			target.SetText(value);
		}

		public override MvxBindingMode DefaultMode => MvxBindingMode.OneWay;
	}
}