using System.Threading.Tasks;

using JKChat.Core.Models;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Base.Items;

using MvvmCross.Commands;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Settings {
	public class MinimapSettingsViewModel : BaseViewModel {
		public IMvxCommand ItemClickCommand { get; init; }

		public MvxObservableCollection<TableGroupedItemVM> Items { get; init; }

		public MinimapSettingsViewModel() {
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
									 Items.Add(groupItem);
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
		}

		private async Task ItemClickExecute(TableItemVM item) {
			await item.ClickCommand.ExecuteAsync();
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