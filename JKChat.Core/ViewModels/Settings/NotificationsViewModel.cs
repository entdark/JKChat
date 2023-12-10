using System;
using System.Threading.Tasks;

using JKChat.Core.Models;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Base;
using JKChat.Core.ViewModels.Base.Items;
using JKChat.Core.ViewModels.Dialog;

using MvvmCross.Commands;
using MvvmCross.Core;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Settings {
	public class NotificationsViewModel : BaseViewModel {
		private readonly INotificationsService notificationsService;
		private readonly TableToggleItemVM notificationsItem;
		private readonly IMvxLifetime lifetime;

		public IMvxCommand ItemClickCommand { get; init; }

		public MvxObservableCollection<TableGroupedItemVM> Items { get; init; }

		private bool notificationsEnabled;
		public bool NotificationsEnabled {
			get => notificationsEnabled;
			set => SetProperty(ref notificationsEnabled, value, () => {
				bool enabled = notificationsService?.NotificationsEnabled ?? false;
				bool add = notificationsEnabled && enabled;
				ToggleOptions(NotificationOptions.Enabled, add);
				if (!notificationsEnabled)
					notificationsItem.IsChecked = false;
			});
		}

		public NotificationsViewModel(INotificationsService notificationsService, IMvxLifetime lifetime) {
			this.notificationsService = notificationsService;
			Title = "Notifications";
			ItemClickCommand = new MvxAsyncCommand<TableItemVM>(ItemClickExecute);
			TableGroupedItemVM groupItem = null;
			var options = AppSettings.NotificationOptions;
			Items = new() {
				new() {
					Items = new() {
						(notificationsItem = new TableToggleItemVM() {
							Title = "Notifications",
							IsChecked = (NotificationsEnabled = options.HasFlag(NotificationOptions.Enabled)),
							Toggled = item => {
								if (item.IsChecked && !Items.Contains(groupItem)) {
									 Items.Add(groupItem);
								} else if (!item.IsChecked && Items.Contains(groupItem)) {
									 Items.Remove(groupItem);
								}
								NotificationsEnabled = item.IsChecked;
							}
						})
					}
				},
				(groupItem = new() {
					Items = new() {
						new TableToggleItemVM() {
							Title = "Player connects",
							IsChecked = options.HasFlag(NotificationOptions.PlayerConnects),
							Toggled = item => {
								ToggleOptions(NotificationOptions.PlayerConnects, item.IsChecked);
							}
						},
						new TableToggleItemVM() {
							Title = "Private message",
							IsChecked = options.HasFlag(NotificationOptions.PrivateMessages),
							Toggled = item => {
								ToggleOptions(NotificationOptions.PrivateMessages, item.IsChecked);
							}
						},
						new TableValueItemVM() {
							Title = "Keywords",
							Value = GetSavedKeywordsValue(),
							OnClick = KeywordsExecute
						}
					}
				})
			};
			if (!NotificationsEnabled) {
				Items.Remove(groupItem);
			}
			this.lifetime = lifetime;
		}

		public override void ViewCreated() {
			base.ViewCreated();
			lifetime.LifetimeChanged -= LifetimeChanged;
			lifetime.LifetimeChanged += LifetimeChanged;
		}

		public override void ViewDestroy(bool viewFinishing = true) {
			if (viewFinishing) {
				lifetime.LifetimeChanged -= LifetimeChanged;
			}
			base.ViewDestroy(viewFinishing);
		}

		public override void ViewAppeared() {
			base.ViewAppeared();
			if (NotificationsEnabled && !notificationsService.NotificationsEnabled) {
				NotificationsEnabled = false;
			}
		}

		private void LifetimeChanged(object sender, MvxLifetimeEventArgs ev) {
			switch (ev.LifetimeEvent) {
			case MvxLifetimeEvent.ActivatedFromMemory:
				ViewAppeared();
				break;
			}
		}

		private async Task ItemClickExecute(TableItemVM item) {
			await item.ClickCommand.ExecuteAsync();
		}

		private async Task KeywordsExecute(TableValueItemVM item) {
			await DialogService.ShowAsync(new JKDialogConfig() {
				Title = "Keywords",
				Message = "Separate with spaces",
				Input = new DialogInputViewModel(GetSavedKeywords()),
				OkText = "OK",
				OkAction = config => {
					string keywords = config.Input?.Text;
					AppSettings.NotificationKeywords = keywords?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					item.Value = GetSavedKeywordsValue();
				},
				CancelText = "Cancel"
			});
		}

		private static void ToggleOptions(NotificationOptions options, bool add) {
			if (add) {
				AppSettings.NotificationOptions |= options;
			} else {
				AppSettings.NotificationOptions &= ~options;
			}
		}
		
		private static string GetSavedKeywords() => AppSettings.NotificationKeywords is { Length: > 0 } keywords ? string.Join(' ', keywords) : string.Empty;
		private static string GetSavedKeywordsValue() => AppSettings.NotificationKeywords is { Length: > 0 } keywords ? string.Join(' ', keywords) : "\u2014";
	}
}

