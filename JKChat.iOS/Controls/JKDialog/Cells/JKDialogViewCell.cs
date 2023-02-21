using System;

using Foundation;

using JKChat.Core.ViewModels.Dialog.Items;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;

using UIKit;

namespace JKChat.iOS.Controls.JKDialog.Cells {
	public partial class JKDialogViewCell : MvxTableViewCell {
		public static readonly NSString Key = new NSString("JKDialogViewCell");
		public static readonly UINib Nib;

		static JKDialogViewCell() {
			Nib = UINib.FromName("JKDialogViewCell", NSBundle.MainBundle);
		}

		protected JKDialogViewCell(IntPtr handle) : base(handle) {
			this.DelayBind(BindingControls);
		}

		private void BindingControls() {
			using var set = this.CreateBindingSet<JKDialogViewCell, DialogItemVM>();
			set.Bind(NameLabel).For(v => v.AttributedText).To(vm => vm.Name).WithConversion("ColourText");
			set.Bind(ContentView).For(v => v.BackgroundColor).To(vm => vm.IsSelected).WithConversion("DialogSelection");
		}
	}
}
