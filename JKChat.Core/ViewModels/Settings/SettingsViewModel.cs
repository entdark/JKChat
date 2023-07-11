using System.Text;
using System.Threading.Tasks;

using JKChat.Core.Messages;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.Dialog.Items;

using MvvmCross.Commands;
using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.ViewModels.Settings {
	public class SettingsViewModel : BaseViewModel {
		private readonly IJKClientService jkclientService;
		private MvxSubscriptionToken playerNameMessageToken;

		public IMvxCommand PlayerNameCommand { get; init; }
		public IMvxCommand EncodingCommand { get; init; }
		public IMvxCommand LocationUpdateCommand { get; init; }

		private string playerName;
		public string PlayerName {
			get => playerName;
			set => SetProperty(ref playerName, value);
		}

		private Encoding encoding;
		public Encoding Encoding {
			get => encoding;
			set => SetProperty(ref encoding, value);
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

		public SettingsViewModel(IJKClientService jkclientService) {
			this.jkclientService = jkclientService;
			Title = "Settings";
			PlayerNameCommand = new MvxAsyncCommand(PlayerNameExecute);
			EncodingCommand = new MvxAsyncCommand(EncodingExecute);
			LocationUpdateCommand = new MvxCommand(LocationUpdateExecute);
			PlayerName = AppSettings.PlayerName;
			Encoding = jkclientService.Encoding;
			locationUpdate = AppSettings.LocationUpdate;
		}

		public override void ViewAppearing() {
			base.ViewAppearing();
		}

		private async Task PlayerNameExecute() {
			string name = AppSettings.PlayerName;
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Choose your name",
				Message = name,
				Input = name,
				RightButton = "OK",
				RightClick = (input) => {
					name = input as string;
				},
				LeftButton = "Cancel",
				Type = JKDialogType.Title | JKDialogType.MessageFromInput
			});
			AppSettings.PlayerName = name;
//			await NavigationService.NavigateFromRoot<SettingsNameViewModel>();
		}

		private async Task EncodingExecute() {
			var dialogList = new DialogListViewModel();
			var availableEncodings = jkclientService.AvailableEncodings;
			for (int i = 0; i < availableEncodings.Length; i++) {
				dialogList.Items.Add(new DialogItemVM() {
					Id = i,
					Name = availableEncodings[i].EncodingName,
					IsSelected = availableEncodings[i].Equals(Encoding)
				});
			}
			int id = -1;
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Select encoding",
				RightButton = "OK",
				RightClick = (input) => {
					if (input is DialogItemVM dialogItem) {
						id = dialogItem.Id;
					}
				},
				LeftButton = "Cancel",
				ListViewModel = dialogList,
				Type = JKDialogType.Title | JKDialogType.List
			});
			jkclientService.SetEncodingById(id);
			Encoding = jkclientService.Encoding;
			AppSettings.EncodingId = id;
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
	public class SettingsViewModel2 : SettingsViewModel {
		public SettingsViewModel2(IJKClientService jkclientService) : base(jkclientService) {}
	}
}
