using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.ServerList.Items;

using JKClient;

using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.ServerList {
	public class ServerListPickerViewModel : BaseViewModel {
		private readonly IServerListService serverListService;

		public IMvxCommand ItemClickCommand { get; init; }

		private MvxObservableCollection<ServerListItemVM> items;
		public MvxObservableCollection<ServerListItemVM> Items {
			get => items;
			set => SetProperty(ref items, value);
		}

		public ServerListPickerViewModel(IServerListService serverListService) {
			this.serverListService = serverListService;
			Items = new MvxObservableCollection<ServerListItemVM>();
			ItemClickCommand = new MvxAsyncCommand(ItemClickExecute);
		}

		protected override Task BackgroundInitialize() {
			return LoadServerList();
		}

		private async Task LoadServerList() {
			IsLoading = true;
			try {
				var servers = await serverListService.GetCurrentList();
				if (servers != null && servers.Any()) {
					var items = servers.Where(server => server.Ping != 0).OrderByDescending(server => server.Clients).Select(SetupItem);
					await Task.Delay(200);
					foreach (var item in items) {
						await InvokeOnMainThreadAsync(() => {
							Items.Add(item);
						});
						//await Task.Delay(50);
					}
				}
			} catch (Exception exception) {
				Debug.WriteLine(exception);
			}
			IsLoading = false;
		}

		private ServerListItemVM SetupItem(ServerInfo server) {
			return new ServerListItemVM(server);
		}

		private async Task ItemClickExecute() {
			await NavigationService.Close(this);
		}
	}
}
