using System.Collections.Generic;

using Foundation;

using JKChat.Core.Models;
using JKChat.Core.ViewModels.ServerList.Items;

using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;

using ObjCRuntime;

using UIKit;

namespace JKChat.iOS.Views.ServerList.Cells {
	public partial class ServerListViewCell : MvxTableViewCell {
		public static readonly NSString Key = new NSString("ServerListViewCell");
		public static readonly UINib Nib;

		private bool needPassword;
		public bool NeedPassword {
			get => needPassword;
			set {
				needPassword = value;
				ConnectButton.SetImage(needPassword ? Theme.Image.Lock_Medium : null, UIControlState.Normal);
			}
		}

		static ServerListViewCell() {
			Nib = UINib.FromName("ServerListViewCell", NSBundle.MainBundle);
		}

		protected ServerListViewCell(NativeHandle handle) : base(handle) {
			this.DelayBind(BindingControls);
		}

		private void BindingControls() {
			var tintedStyle = UIButtonConfiguration.TintedButtonConfiguration;
			tintedStyle.CornerStyle = UIButtonConfigurationCornerStyle.Capsule;
			var grayStyle = UIButtonConfiguration.GrayButtonConfiguration;
			grayStyle.CornerStyle = UIButtonConfigurationCornerStyle.Capsule;
			using var set = this.CreateBindingSet<ServerListViewCell, ServerListItemVM>();
			set.Bind(ServerNameLabel).For(v => v.AttributedText).To(vm => vm.ServerName).WithConversion("ColourText");
			set.Bind(MapNameLabel).For(v => v.Text).To(vm => vm.MapName);
			set.Bind(PlayersLabel).For(v => v.Text).To("Format('{0} players', Players)");
			set.Bind(GameLabel).For(v => v.AttributedText).To("ColourText(If(Modification, Format('{0} - {1}', GameName, Modification), GameName))");
			set.Bind(FavouriteButton).For(v => v.Selected).To(vm => vm.IsFavourite);
			set.Bind(StatusLabel).For(v => v.Text).To(vm => vm.Status);
			set.Bind(StatusLabel).For(v => v.TextColor).To(vm => vm.Status).WithDictionaryConversion(new Dictionary<ConnectionStatus, UIColor>() {
				[ConnectionStatus.Connected] = UIColor.Label
			}, UIColor.SecondaryLabel);
			set.Bind(StatusImageView).For(v => v.TintColor).To(vm => vm.Status).WithConversion("ConnectionColor");
			set.Bind(ConnectButton).To(vm => vm.ConnectCommand);
			set.Bind(ConnectButton).For(v => v.Configuration).To(vm => vm.Status).WithDictionaryConversion(new Dictionary<ConnectionStatus, UIButtonConfiguration>() {
				[ConnectionStatus.Disconnected] = tintedStyle
			}, grayStyle);
			set.Bind(ConnectButton).For("Title").To("If(EnumBool(Status, 'Disconnected'), 'Connect', 'Disconnect')");
			set.Bind(this).For(v => v.NeedPassword).To(vm => vm.NeedPassword);
			set.Bind(PreviewImageView).For(v => v.Image).To(vm => vm.Game).WithDictionaryConversion(new Dictionary<Game, UIImage>() {
				[Game.JediAcademy] = Theme.Image.JAPreviewBackground,
				[Game.JediOutcast] = Theme.Image.JOPreviewBackground,
				[Game.Quake3] = Theme.Image.Q3PreviewBackground
			}, null);
			set.Bind(PreviewView).For("Visibility").To("EnumBool(Game, 'Unknown')").WithConversion("InvertedVisibility");
		}
	}
}
