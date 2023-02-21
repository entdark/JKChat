using System.Threading.Tasks;

using JKChat.Core.Messages;
using JKChat.Core.ViewModels.Base;

using MvvmCross.Commands;
using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.ViewModels.Settings {
	public class SettingsNameViewModel : BaseViewModel {
		public IMvxCommand ApplyCommand { get; init; }

		private string playerName;
		public string PlayerName {
			get => playerName;
			set => SetProperty(ref playerName, value);
		}

		public SettingsNameViewModel() {
			Title = "Player Name";
			PlayerName = AppSettings.PlayerName;
			ApplyCommand = new MvxAsyncCommand(ApplyExecute);
		}

		private async Task ApplyExecute() {
			AppSettings.PlayerName = PlayerName;
			await NavigationService.Close(this);
		}
	}
}
