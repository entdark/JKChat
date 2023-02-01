using System.Threading.Tasks;

using JKChat.Core.Messages;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;

using MvvmCross.Commands;
using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.ViewModels.Settings {
	public class SettingsViewModel : BaseViewModel {
		private MvxSubscriptionToken playerNameMessageToken;

		public IMvxCommand PlayerNameCommand { get; private set; }
		public IMvxCommand LocationUpdateCommand { get; private set; }

		private string playerName;
		public string PlayerName {
			get => playerName;
			set => SetProperty(ref playerName, value);
		}

		private bool locationUpdate;
		public bool LocationUpdate {
			get => locationUpdate;
			set {
				if (SetProperty(ref locationUpdate, value)) {
					AppSettings.LocationUpdate = value;
				}
			}
		}

		public SettingsViewModel() {
			Title = "Settings";
			PlayerNameCommand = new MvxAsyncCommand(PlayerNameExecute);
			LocationUpdateCommand = new MvxCommand(LocationUpdateExecute);
			locationUpdate = AppSettings.LocationUpdate;
		}

		public override void ViewAppearing() {
			base.ViewAppearing();
			PlayerName = AppSettings.PlayerName;
		}

		private async Task PlayerNameExecute() {
/*			string name = AppSettings.PlayerName;
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Choose your name",
				Input = name,
				RightButton = "OK",
				RightClick = (input) => {
					name = input as string;
				},
				Type = JKDialogType.Title | JKDialogType.Input
			});
			AppSettings.PlayerName = name;*/
			await NavigationService.NavigateFromRoot<SettingsNameViewModel>();
		}

		private void OnPlayerNameMessage(PlayerNameMessage message) {
			PlayerName = AppSettings.PlayerName;
		}

		private void LocationUpdateExecute() {
			LocationUpdate = !LocationUpdate;
		}

		public override void ViewCreated() {
			base.ViewCreated();
			if (playerNameMessageToken == null) {
				playerNameMessageToken = Messenger.Subscribe<PlayerNameMessage>(OnPlayerNameMessage);
			}
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				if (playerNameMessageToken != null) {
					Messenger.Unsubscribe<ServerInfoMessage>(playerNameMessageToken);
					playerNameMessageToken = null;
				}
			}
			base.ViewDestroy(viewFinishing);
		}
	}
	public class SettingsViewModel2 : SettingsViewModel { }
}
