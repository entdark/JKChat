using System;
using System.Linq;
using System.Threading.Tasks;

using JKChat.Core.Helpers;
using JKChat.Core.Models;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Base.Items;

using Microsoft.Maui.Devices;

using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Settings {
	public class MinimapSettingsViewModel : BaseViewModel {
		private readonly TableGroupedItemVM mapProgressesGroup;

		public IMvxCommand ItemClickCommand { get; init; }

		public MvxObservableCollection<TableGroupedItemVM> Items { get; init; }

		public MinimapSettingsViewModel(IMinimapService minimapService) {
			Title = "Minimap";
			ItemClickCommand = new MvxAsyncCommand<TableItemVM>(ItemClickExecute);
			TableGroupedItemVM groupItem = null;
			var options = AppSettings.MinimapOptions;
			Items = new() {
				new() {
					Items = new() {
						new TableToggleItemVM() {
							Title = "Show minimap",
							IsChecked = options.HasFlag(MinimapOptions.Enabled),
							Toggled = item => {
								ToggleOptions(MinimapOptions.Enabled, item.IsChecked);
								if (item.IsChecked && !Items.Contains(groupItem)) {
									 Items.Insert(1, groupItem);
								} else if (!item.IsChecked && Items.Contains(groupItem)) {
									 Items.Remove(groupItem);
								}
							}
						}
					}
				},
				(groupItem = new() {
					Items = new() {
						new TableToggleItemVM() {
							Title = "Show players",
							IsChecked = options.HasFlag(MinimapOptions.Players),
							Toggled = item => {
								ToggleOptions(MinimapOptions.Players, item.IsChecked);
							}
						},
						new TableToggleItemVM() {
							Title = "Show player names",
							IsChecked = options.HasFlag(MinimapOptions.Names),
							Toggled = item => {
								ToggleOptions(MinimapOptions.Names, item.IsChecked);
							}
						},
						new TableToggleItemVM() {
							Title = "Show weapon shots",
							IsChecked = options.HasFlag(MinimapOptions.Weapons),
							Toggled = item => {
								ToggleOptions(MinimapOptions.Weapons, item.IsChecked);
							}
						},
						new TableToggleItemVM() {
							Title = "Show flags (CTF/CTY)",
							IsChecked = options.HasFlag(MinimapOptions.Flags),
							Toggled = item => {
								ToggleOptions(MinimapOptions.Flags, item.IsChecked);
							}
						},
						new TableToggleItemVM() {
							Title = "Show myself",
							IsChecked = options.HasFlag(MinimapOptions.Predicted),
							Toggled = item => {
								ToggleOptions(MinimapOptions.Predicted, item.IsChecked);
							}
						},
						new TableToggleItemVM() {
							Title = "Unfocus on the first message",
							IsChecked = options.HasFlag(MinimapOptions.FirstUnfocus),
							Toggled = item => {
								ToggleOptions(MinimapOptions.FirstUnfocus, item.IsChecked);
							}
						},
						new TableToggleItemVM() {
							Title = "Remember focus state",
							IsChecked = options.HasFlag(MinimapOptions.RememberFocus),
							Toggled = item => {
								ToggleOptions(MinimapOptions.RememberFocus, item.IsChecked);
							}
						},
						new TableToggleItemVM() {
							Title = "Autodownload maps",
							IsChecked = options.HasFlag(MinimapOptions.AutoDownload),
							Toggled = item => {
								ToggleOptions(MinimapOptions.AutoDownload, item.IsChecked);
							}
						},
						new TableValueItemVM() {
							Title = "Download URL",
							Value = AppSettings.MinimapDownloadURL,
							OnClick = DownloadURLExecute
						},
						new TableValueItemVM() {
							Title = "Server info key download URL",
							Value = AppSettings.MinimapServerInfoKeyDownloadURL,
							OnClick = ServerInfoKeyDownloadURLExecute
						},
						new TableValueItemVM() {
							Title = "Minimap size (in pixels)",
							Value = $"{AppSettings.MinimapSize}×{AppSettings.MinimapSize}",
							OnClick = MinimapSizeExecute
						}
					}
				})
			};
			if (DeviceInfo.Platform == DevicePlatform.Android) {
				groupItem.Items.Add(
					new TableToggleItemVM() {
						Title = "High performance",
						IsChecked = options.HasFlag(MinimapOptions.HighPerformance),
						Toggled = item => {
							if (item.IsChecked) {
								DialogService.Show(new() {
									Title = "Warning",
									Message = "The high performance mode uses a hack that can affect battery, warm up the device and might not higher the performance at all.\n\nDo you still want to enable high performance?",
									OkText = "Enable",
									OkAction = _ => {
										ToggleOptions(MinimapOptions.HighPerformance, true);
									},
									CancelText = "Cancel",
									CancelAction = _ => {
										item.IsChecked = false;
									}
								});
							} else {
								ToggleOptions(MinimapOptions.HighPerformance, false);
							}
						}
					}
				);
			}
			if (!options.HasFlag(MinimapOptions.Enabled)) {
				Items.Remove(groupItem);
			}
			var activeMapProgresses = minimapService.GetActiveMapProgresses();
			mapProgressesGroup = new TableGroupedItemVM() {
				Items = new()
			};
			foreach (var mapProgress in activeMapProgresses) {
				mapProgressesGroup.Items.Add(new TableValueItemVM() {
					Title = mapProgress.Key,
					Value = mapProgress.Value.Progress.ToPercentString(),
					OnClick = MapProgressClickExecute,
					Data = mapProgress.Value
				});
			}
			if (mapProgressesGroup.Items.Count > 0) {
				Items.Add(mapProgressesGroup);
			}
		}

		public override void ViewAppearing() {
			base.ViewAppearing();
			foreach (var item in mapProgressesGroup.Items) {
				if (item.Data is MapProgressData mapProgress) {
					mapProgress.ProgressChanged += MapLoadingProgressChanged;
				}
			}
		}

		public override void ViewDisappearing() {
			foreach (var item in mapProgressesGroup.Items) {
				if (item.Data is MapProgressData mapProgress) {
					mapProgress.ProgressChanged -= MapLoadingProgressChanged;
				}
			}
			base.ViewDisappearing();
		}

		private void MapLoadingProgressChanged(MapProgressData mapProgress) {
			var item = mapProgressesGroup.Items.FirstOrDefault(item => object.ReferenceEquals(item.Data, mapProgress));
			if (item is TableValueItemVM valueItem) {
				if (mapProgress.Progress == 0.0f) {
					mapProgressesGroup.Items.Remove(item);
					Items.Remove(mapProgressesGroup);
					Items.Add(mapProgressesGroup);
				} else {
					valueItem.Value = mapProgress.Progress.ToPercentString();
				}
			}
		}

		private async Task ItemClickExecute(TableItemVM item) {
			await item.ClickCommand.ExecuteAsync();
		}

		private Task MapProgressClickExecute(TableValueItemVM item) {
			(item.Data as MapProgressData)?.OfferCancel();
			return Task.CompletedTask;
		}

		private async Task DownloadURLExecute(TableValueItemVM item) {
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Map download URL",
				Input = new(item.Value),
				OkText = "OK",
				OkAction = config => {
					AppSettings.MinimapDownloadURL = item.Value = config.Input?.Text;
				},
				CancelText = "Cancel"
			});
		}

		private async Task ServerInfoKeyDownloadURLExecute(TableValueItemVM item) {
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Server info key map download URL",
				Input = new(item.Value),
				OkText = "OK",
				OkAction = config => {
					AppSettings.MinimapServerInfoKeyDownloadURL = item.Value = config.Input?.Text;
				},
				CancelText = "Cancel"
			});
		}

		private async Task MinimapSizeExecute(TableValueItemVM item) {
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Minimap size",
				Input = new(AppSettings.MinimapSize.ToString()),
				OkText = "OK",
				OkAction = config => {
					if (int.TryParse(config.Input?.Text, out int i)) {
						i = Math.Clamp(i, AppSettings.MinimapMinSize, AppSettings.MinimapMaxSize);
						item.Value = $"{i}×{i}";
						AppSettings.MinimapSize = i;
					}
				},
				CancelText = "Cancel"
			});
		}

		private static void ToggleOptions(MinimapOptions options, bool add) {
			if (add) {
				AppSettings.MinimapOptions |= options;
			} else {
				AppSettings.MinimapOptions &= ~options;
			}
		}
	}
}