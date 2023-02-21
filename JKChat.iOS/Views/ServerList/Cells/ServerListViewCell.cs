using System;

using Foundation;
using JKChat.Core.ViewModels.ServerList.Items;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using UIKit;

namespace JKChat.iOS.Views.ServerList.Cells {
	public partial class ServerListViewCell : MvxTableViewCell {
		public static readonly NSString Key = new NSString("ServerListViewCell");
		public static readonly UINib Nib;

		static ServerListViewCell() {
			Nib = UINib.FromName("ServerListViewCell", NSBundle.MainBundle);
		}

		protected ServerListViewCell(IntPtr handle) : base(handle) {
			this.DelayBind(BindingControls);
		}

		private void BindingControls() {
			using var set = this.CreateBindingSet<ServerListViewCell, ServerListItemVM>();
			set.Bind(PasswordImageView).For("Visibility").To(vm => vm.NeedPassword).WithConversion("Visibility");
			set.Bind(ServerNameLabel).For(v => v.AttributedText).To(vm => vm.ServerName).WithConversion("ColourText");
			set.Bind(MapNameLabel).For(v => v.Text).To(vm => vm.MapName);
			set.Bind(PlayersLabel).For(v => v.Text).To(vm => vm.Players);
			set.Bind(GameTypeLabel).For(v => v.Text).To(vm => vm.GameType);
			set.Bind(PingLabel).For(v => v.Text).To(vm => vm.Ping);
			set.Bind(StatusLabel).For(v => v.Text).To(vm => vm.Status);
			set.Bind(StatusView).For(v => v.BackgroundColor).To(vm => vm.Status).WithConversion("ConnectionColor");
		}
	}
}
