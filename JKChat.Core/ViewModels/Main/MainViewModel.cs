using System;
using System.Collections.Generic;
using System.Text;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.ServerList;
using MvvmCross.Commands;

namespace JKChat.Core.ViewModels.Main {
	public class MainViewModel : BaseViewModel {
		public IMvxCommand ShowServerListCommand { get; private set; }
		public MainViewModel() {
			ShowServerListCommand = new MvxAsyncCommand(async () => { await NavigationService.Navigate<ServerListViewModel>(); });
		}
	}
}
