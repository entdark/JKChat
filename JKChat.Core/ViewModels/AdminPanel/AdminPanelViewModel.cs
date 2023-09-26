using System.Threading.Tasks;

using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.ServerList;
using JKChat.Core.ViewModels.ServerList.Items;

using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.AdminPanel {
	public class AdminPanelViewModel : BaseViewModel {
		public IMvxCommand AddServerCommand { get; init; }
		public IMvxCommand SelectServerCommand { get; init; }

		private MvxObservableCollection<ServerListItemVM> items;
		public MvxObservableCollection<ServerListItemVM> Items {
			get => items;
			set => SetProperty(ref items, value);
		}

		public AdminPanelViewModel() {
			Title = "Admin panel";
			AddServerCommand = new MvxAsyncCommand(AddServerExecute);
			SelectServerCommand = new MvxAsyncCommand(SelectServerExecute);
			Items = new MvxObservableCollection<ServerListItemVM>();
		}

		private async Task AddServerExecute() {
			string address = string.Empty;
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Enter server address",
				Input = new(address),
				OkText = "OK",
				OkAction = config => {
					address = config?.Input?.Text;
				},
				CancelText = "Cancel"
			});
		}

		private async Task SelectServerExecute() {
			await NavigationService.Navigate<ServerListPickerViewModel>();
		}
	}
}
