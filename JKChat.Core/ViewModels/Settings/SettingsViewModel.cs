using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JKChat.Core.Helpers;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Base.Items;
using JKChat.Core.ViewModels.Dialog;
using JKChat.Core.ViewModels.Dialog.Items;

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using MvvmCross;
using MvvmCross.Commands;
using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.ViewModels.Settings {
	public class SettingsViewModel : BaseViewModel {
		private readonly IJKClientService jkclientService;
		private readonly TableValueItemVM playerNameItem;
		private MvxSubscriptionToken playerNameMessageToken;
		
		public IMvxCommand ItemClickCommand { get; init; }
		public IMvxCommand PrivacyPolicyCommand { get; init; }

		public List<TableGroupedItemVM> Items { get; init; }

		public SettingsViewModel(IJKClientService jkclientService) {
			this.jkclientService = jkclientService;
			Title = "Settings";
			ItemClickCommand = new MvxAsyncCommand<TableItemVM>(ItemClickExecute);
			PrivacyPolicyCommand = new MvxAsyncCommand(PrivacyPolicyExecute);
			Items = new() {
				new() {
					Items = new() {
						(playerNameItem = new() {
							Title = "Player Name",
							Value = AppSettings.PlayerName,
							OnClick = PlayerNameExecute
						}),
						new TableToggleItemVM() {
							Title = "OpenJK Colours",
							IsChecked = AppSettings.OpenJKColours,
							Toggled = item => {
								AppSettings.OpenJKColours = item.IsChecked;
								playerNameItem.Value = string.Empty;
								playerNameItem.Value = AppSettings.PlayerName;
							}
						},
						new TableValueItemVM() {
							Title = "Encoding",
							Value = jkclientService.Encoding.EncodingName,
							OnClick = EncodingExecute
						}
					}
				},
				new() {
					Items = new() {
						new TableNavigationItemVM() {
							Title = "Notifications",
							OnClick = NotificationsExecute
						},
						new TableValueItemVM() {
							Title = "Widget Navigation",
							Value = AppSettings.WidgetLink.ToDisplayString(),
							OnClick = WidgetLinkExecute
						},
						new TableValueItemVM() {
							Title = "Theme",
							Value = AppSettings.AppTheme.ToString(),
							OnClick = AppThemeExecute
						}
					}
				}
			};
			if (DeviceInfo.Platform.IsApple()) {
				Items.Add(new() {
					Items = new() {
						new TableToggleItemVM() {
							Title = "Location Updates",
							IsChecked = AppSettings.LocationUpdate,
							Toggled = item => AppSettings.LocationUpdate = item.IsChecked
						}
					}
				});
			}
			playerNameMessageToken ??= Messenger.Subscribe<PlayerNameMessage>(OnPlayerNameMessage);
		}

		private async Task ItemClickExecute(TableItemVM item) {
			await item.ClickCommand.ExecuteAsync();
		}

		private async Task NotificationsExecute(TableNavigationItemVM item) {
			await NavigationService.NavigateFromRoot<NotificationsViewModel>();
		}

		private async Task PlayerNameExecute(TableValueItemVM item) {
			string name = AppSettings.PlayerName;
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Enter Player Name",
				Input = new DialogInputViewModel(name, true),
				OkText = "OK",
				OkAction = config => {
					AppSettings.PlayerName = config?.Input?.Text;
				},
				CancelText = "Cancel"
			});
//			await NavigationService.NavigateFromRoot<SettingsNameViewModel>();
		}

		private async Task EncodingExecute(TableValueItemVM item) {
			var availableEncodings = jkclientService.AvailableEncodings;
			var dialogList = new DialogListViewModel(availableEncodings.Select(encoding => {
				return new DialogItemVM() {
					Name = encoding.EncodingName,
					IsSelected = encoding.Equals(jkclientService.Encoding)
				};
			}), DialogSelectionType.SingleSelection);
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Select Encoding",
				CancelText = "Cancel",
				OkText = "OK",
				OkAction = config => {
					if (config?.List?.SelectedIndex is int id && id >= 0) {
						jkclientService.SetEncodingById(id);
						item.Value = jkclientService.Encoding.EncodingName;
						AppSettings.EncodingId = id;
					}
				},
				List = dialogList
			});
		}

		private async Task WidgetLinkExecute(TableValueItemVM item) {
			var dialogList = new DialogListViewModel(Enum.GetValues<WidgetLink>().Select(widgetLink => {
				return new DialogItemVM() {
					Id = widgetLink,
					Name = widgetLink.ToDisplayString(),
					IsSelected = widgetLink == AppSettings.WidgetLink
				};
			}), DialogSelectionType.SingleSelection);
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Widget Navigation",
				CancelText = "Cancel",
				OkText = "OK",
				OkAction = config => {
					if (config?.List?.SelectedItem is { } selectedItem) {
						var widgetLink = (WidgetLink)selectedItem.Id;
						AppSettings.WidgetLink = widgetLink;
						item.Value = widgetLink.ToDisplayString();
					}
				},
				List = dialogList
			});
		}

		private async Task AppThemeExecute(TableValueItemVM item) {
			var dialogList = new DialogListViewModel(Enum.GetValues<Models.AppTheme>().Select(theme => {
				return new DialogItemVM() {
					Id = theme,
					Name = theme.ToString(),
					IsSelected = theme == AppSettings.AppTheme
				};
			}), DialogSelectionType.SingleSelection);
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Select Theme",
				CancelText = "Cancel",
				OkText = "OK",
				OkAction = config => {
					if (config?.List?.SelectedItem is { } selectedItem) {
						var theme = (Models.AppTheme)selectedItem.Id;
						AppSettings.AppTheme = theme;
						item.Value = theme.ToString();
					}
				},
				List = dialogList
			});
		}

		private void OnPlayerNameMessage(PlayerNameMessage message) {
			playerNameItem.Value = AppSettings.PlayerName;
		}

		private async Task PrivacyPolicyExecute() {
			try {
				await Browser.OpenAsync("https://github.com/entdark/JKChat/blob/master/jkchat-terms-conditions.md");
			} catch {}
		}

		public override void ViewCreated() {
			base.ViewCreated();
			playerNameMessageToken ??= Messenger.Subscribe<PlayerNameMessage>(OnPlayerNameMessage);
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
}
