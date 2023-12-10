using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Models;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Base.Items;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.Dialog.Items;

using MvvmCross.Commands;

namespace JKChat.Core.ViewModels.ServerList {
	public class FilterViewModel : BaseViewModel<Filter> {
		private readonly TableToggleItemVM showEmptyItem, showFullItem;
		private readonly TableValueItemVM gameItem, gameTypeItem, gameModItem;

		public IMvxCommand ResetCommand { get; init; }
		public IMvxCommand ItemClickCommand { get; init; }

		public Filter Filter { get; private set; }

		public List<TableGroupedItemVM> Items { get; init; }

		public FilterViewModel() {
			Title = "Filter";
			ResetCommand = new MvxCommand(ResetExecute, ResetCanExecute);
			ItemClickCommand = new MvxAsyncCommand<TableItemVM>(ItemClickExecute);
			Items = new() {
				new() {
					Items = new() {
						(showEmptyItem = new TableToggleItemVM() {
							Title = "Not entirely empty ",
							Toggled = item => Filter.ShowEmpty = item.IsChecked
						}),
						(showFullItem = new TableToggleItemVM() {
							Title = "Not entirely full ",
							Toggled = item => Filter.ShowFull = item.IsChecked
						})
					}
				},
				new() {
					Items = new() {
						(gameItem = new TableValueItemVM() {
							Title = "Game",
							OnClick = GameExecute
						}),
						(gameTypeItem = new TableValueItemVM() {
							Title = "Game type",
							OnClick = GameTypeExecute
						}),
						(gameModItem = new TableValueItemVM() {
							Title = "Game Mod",
							OnClick = GameModExecute
						})
					}
				}
			};
		}

		public override void Prepare(Filter parameter) {
			Filter = parameter;
			SetFilterProperties();
		}

		private void FilterPropertyChanged(object sender, PropertyChangedEventArgs ev) {
			SetFilterProperties();
			ResetCommand.RaiseCanExecuteChanged();
		}

		private void SetFilterProperties() {
			int gameCount = Filter.Game.CountVersions();
			string game = gameCount != ClientVersionExtensions.Versions.Length ? gameCount.ToString() : Filter.Game.ToDisplayString();
			int gameTypeCount = Filter.GameType.CountGameTypes();
			string gameType = gameTypeCount != GameTypeExtensions.GameTypes.Length ? gameTypeCount.ToString() : Filter.GameType.ToDisplayString(Filter.Game);
			var selectedGameMods = Filter.GameMod.Where(gm => gm.Value).Select(gm => HandleEmptyGameMod(gm.Key));
			int selectedGameModsCount = selectedGameMods.Count();
			string gameMod = selectedGameModsCount switch {
				0 => "All", //can happen only if initial server list loading has failed
				1 => selectedGameMods.FirstOrDefault() ?? string.Empty, //should never be null
				_ when Filter.GameMod.Count == selectedGameModsCount => "All",
				_ => selectedGameModsCount.ToString()
			};
			showEmptyItem.IsChecked = Filter.ShowEmpty;
			showFullItem.IsChecked = Filter.ShowFull;
			gameItem.Value = game;
			gameTypeItem.Value = gameType;
			gameModItem.Value = gameMod;
		}

		private void ResetExecute() {
			Filter.Reset();
		}

		private async Task ItemClickExecute(TableItemVM item) {
			await item.ClickCommand.ExecuteAsync();
		}

		private bool ResetCanExecute() {
			return !Filter.IsReset;
		}

		private async Task GameExecute(TableValueItemVM item) {
			await DialogService.ShowAsync(new DialogListViewModel(ClientVersionExtensions.Versions.Select(version => new DialogItemVM() {
				Id = version.ToBitField(),
				Name = version.ToDisplayString(),
				IsSelected = Filter.Game != ClientVersionExtensions.ClientVersionAll && Filter.Game.HasField(version)
			}).OrderByDescending(item => item.IsSelected), DialogSelectionType.MultiSelection), config => {
				int version = config.List.SelectedItems.Aggregate(0, (game, item) => game | (int)item.Id);
				Filter.Game = version == 0 ? ClientVersionExtensions.ClientVersionAll : version;
			}, "Choose Game");
		}

		private async Task GameTypeExecute(TableValueItemVM item) {
			await DialogService.ShowAsync(new DialogListViewModel(GameTypeExtensions.GameTypes.Select(gameType => new DialogItemVM() {
				Id = gameType.ToBitField(),
				Name = gameType.ToDisplayString(Filter.Game),
				IsSelected = Filter.GameType != GameTypeExtensions.GameTypeAll && Filter.GameType.HasField(gameType)
			}).OrderByDescending(item => item.IsSelected), DialogSelectionType.MultiSelection), config => {
				int gameType = config.List.SelectedItems.Aggregate(0, (type, item) => type | (int)item.Id);
				Filter.GameType = gameType == 0 ? GameTypeExtensions.GameTypeAll : gameType;
			}, "Choose Game Type");
		}

		private async Task GameModExecute(TableValueItemVM item) {
			if (Filter.GameMod.Count <= 0)
				return;
			bool allGameModsSelected = Filter.GameMod.All(gameMod => gameMod.Value);
			await DialogService.ShowAsync(new DialogListViewModel(Filter.GameMod.Select(gameMod => new DialogItemVM() {
				Id = gameMod.Key,
				Name = HandleEmptyGameMod(gameMod.Key),
				IsSelected = !allGameModsSelected && gameMod.Value
			}).OrderByDescending(item => item.IsSelected), DialogSelectionType.MultiSelection), config => {
				var selectedItems = config.List.Items.ToDictionary(item => item.Id as string, item => item.IsSelected);
				if (!selectedItems.Any(item => item.Value)) {
					Filter.ResetGameMod(false);
				} else {
					Filter.SetGameMods(selectedItems);
				}
			}, "Choose Game Mod");
		}

		public override void ViewCreated() {
			base.ViewCreated();
			Filter.PropertyChanged -= FilterPropertyChanged;
			Filter.PropertyChanged += FilterPropertyChanged;
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				Filter.PropertyChanged -= FilterPropertyChanged;
			}
			base.ViewDestroy(viewFinishing);
		}

		private static string HandleEmptyGameMod(string gameMod) => !string.IsNullOrEmpty(gameMod) ? gameMod : "Without mod";
	}
}