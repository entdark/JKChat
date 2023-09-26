using System.Collections.Generic;

using Foundation;

using JKChat.Core.ViewModels.Dialog.Items;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Controls.JKDialog.Cells {
	public partial class JKDialogViewCell : MvxTableViewCell {
		public static readonly NSString Key = new NSString("JKDialogViewCell");
		public static readonly UINib Nib;

		static JKDialogViewCell() {
			Nib = UINib.FromName("JKDialogViewCell", NSBundle.MainBundle);
		}

		protected JKDialogViewCell(NativeHandle handle) : base(handle) {
			this.DelayBind(BindingControls);
		}

		private void BindingControls() {
			using var set = this.CreateBindingSet<JKDialogViewCell, DialogItemVM>();
			set.Bind(TextLabel).For(v => v.AttributedText).To(vm => vm.Name).WithConversion("ColourText");
			set.Bind(this).For(v => v.Accessory).To(vm => vm.IsSelected).WithDictionaryConversion(new Dictionary<bool, UITableViewCellAccessory>() {
				[true] = UITableViewCellAccessory.Checkmark,
				[false] = UITableViewCellAccessory.None
			});
		}
	}
}