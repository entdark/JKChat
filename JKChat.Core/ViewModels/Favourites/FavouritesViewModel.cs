﻿using System;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Messages;
using JKChat.Core.Navigation.Parameters;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.ServerList.Items;

using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Favourites {
	public class FavouritesViewModel : BaseServerViewModel {
		private readonly ICacheService cacheService;
		private readonly IServerListService serverListService;
		private readonly IGameClientsService gameClientsService;

		public IMvxCommand ItemClickCommand { get; init; }
		public IMvxCommand RefreshCommand { get; init; }
		public IMvxCommand AddServerCommand { get; init; }

		public MvxObservableCollection<ServerListItemVM> Items { get; init; }

		private bool isRefreshing;
		public bool IsRefreshing {
			get => isRefreshing;
			set => SetProperty(ref isRefreshing, value);
		}

		public FavouritesViewModel(ICacheService cacheService, IServerListService serverListService, IGameClientsService gameClientsService) {
			Title = "Favourites";
			ItemClickCommand = new MvxAsyncCommand<ServerListItemVM>(ItemClickExecute);
			RefreshCommand = new MvxAsyncCommand(RefreshExecute);
			Items = new();
			this.cacheService = cacheService;
			this.serverListService = serverListService;
			this.gameClientsService = gameClientsService;
		}

		protected override void OnServerInfoMessage(ServerInfoMessage message) {
			base.OnServerInfoMessage(message);
			var item = Items.FirstOrDefault(it => it.ServerInfo == message.ServerInfo);
			if (item != null) {
				if (message.Status.HasValue
					&& message.Status.Value != Models.ConnectionStatus.Disconnected
					&& item.Status == Models.ConnectionStatus.Disconnected) {
					Items.Move(Items.IndexOf(item), 0);
				}
				item.Set(message.ServerInfo, message.Status);
			}
		}

		protected override void OnFavouriteMessage(FavouriteMessage message) {
			base.OnFavouriteMessage(message);
			var item = Items.FirstOrDefault(it => it.ServerInfo == message.ServerInfo);
			if (item != null && !message.IsFavourite) {
				Items.Remove(item);
			} else if (item == null && message.IsFavourite) {
				var server = new ServerListItemVM(message.ServerInfo);
				server.SetFavourite(true);
				var gameClient = gameClientsService.GetClient(message.ServerInfo);
				if (gameClient?.Status != Models.ConnectionStatus.Disconnected) {
					Items.Insert(0, server);
				} else {
					Items.Add(server);
				}
			}
		}

		private async Task RefreshExecute() {
			if (IsLoading) {
				return;
			}
			IsRefreshing = true;
			try {
				var favouriteServers = (await cacheService.LoadFavouriteServers()).ToArray();
				if (favouriteServers.Length > 0) {
					var serverInfos = await serverListService.RefreshList(favouriteServers.Select(s => s.ServerInfo));
					foreach (var serverInfo in serverInfos) {
						var item = favouriteServers.FirstOrDefault(s => s.ServerInfo == serverInfo);
						item?.Set(serverInfo);
					}
				}
				Items.ReplaceWith(favouriteServers);
			} catch (Exception exception) {
				Helpers.Common.ExceptionCallback(exception);
			}
			IsRefreshing = false;
		}

		private async Task ItemClickExecute(ServerListItemVM item) {
			if (item.Status != Models.ConnectionStatus.Disconnected) {
				await NavigationService.NavigateFromRoot<ChatViewModel, ServerInfoParameter>(new(item), item.ServerInfo);
				Items.Move(Items.IndexOf(item), 0);
			} else {
				await NavigationService.NavigateFromRoot<ServerInfoViewModel, ServerInfoParameter>(new(item) { LoadInfo = true }, item.ServerInfo);
			}
		}

		protected override async Task BackgroundInitialize() {
			await LoadServerList();
		}

		private async Task LoadServerList() {
			IsLoading = true;
			try {
				var favouriteServers = await cacheService.LoadFavouriteServers();
				Items.ReplaceWith(favouriteServers);
			} catch (Exception exception) {
				Helpers.Common.ExceptionCallback(exception);
			}
			IsLoading = false;
		}

		public override void ViewDisappearing() {
			base.ViewDisappearing();
			if (Items.Count <= 0)
				return;
			var favouriteItems = Items.Where(item => item.IsFavourite).ToArray();
			if (favouriteItems.Length > 0 && favouriteItems.Length != Items.Count) {
				Items.ReplaceWith(favouriteItems);
			}
		}
	}
}