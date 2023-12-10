using Foundation;

using JKChat.Core.ViewModels.Settings;
using JKChat.iOS.Services;
using JKChat.iOS.Views.Base;
using JKChat.iOS.ViewSources;

using MvvmCross.Platforms.Ios.Presenters.Attributes;

using UIKit;

using UserNotifications;

namespace JKChat.iOS.Views.Settings;

[MvxSplitViewPresentation(MasterDetailPosition.Detail, WrapInNavigationController = true)]
public partial class NotificationsViewController : BaseViewController<NotificationsViewModel> {
	private bool notificationsEnabled;
	public bool NotificationsEnabled {
		get => notificationsEnabled;
		set {
			notificationsEnabled = value;
			CheckNotificationsPermission(notificationsEnabled);
		}
	}

	public NotificationsViewController() : base(nameof(NotificationsViewController), null) {
	}

	public override void ViewDidLoad() {
		base.ViewDidLoad();

		var source = new TableGroupedViewSource(NotificationsTableView);

		using var set = this.CreateBindingSet();
		set.Bind(source).For(s => s.ItemsSource).To(vm => vm.Items);
		set.Bind(source).For(s => s.SelectionChangedCommand).To(vm => vm.ItemClickCommand);
		set.Bind(this).For(v => v.NotificationsEnabled).To(vm => vm.NotificationsEnabled);
	}

	public override void ViewWillAppear(bool animated) {
		base.ViewWillAppear(animated);

		NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Always;
		NavigationController.NavigationBar.PrefersLargeTitles = true;
	}

	private void CheckNotificationsPermission(bool enabled) {
		if (!enabled)
			return;
		UNUserNotificationCenter.Current.GetNotificationSettings(settings => {
			var status = settings.AuthorizationStatus;
			switch (status) {
			case UNAuthorizationStatus.Authorized:
			case UNAuthorizationStatus.Provisional:
			case UNAuthorizationStatus.Ephemeral:
				//we are good
				break;
			case UNAuthorizationStatus.NotDetermined:
				UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound | UNAuthorizationOptions.Provisional, (approved, error) => {
					ViewModel.NotificationsEnabled = approved;
				});
				break;
			case UNAuthorizationStatus.Denied:
				DialogService.Show(new() {
					Title = "Notifications disabled",
					Message = "Go to application settings to enable notifications",
					OkText = "Settings",
					CancelText = "Cancel",
					OkAction = _ => {
						ViewModel.NotificationsEnabled = false;
						try {
							var url = new NSUrl(UIApplication.OpenSettingsUrlString);
							if (UIApplication.SharedApplication.CanOpenUrl(url))
								UIApplication.SharedApplication.OpenUrl(url, new UIApplicationOpenUrlOptions(), null);
						} catch {}
					},
					CancelAction = _ => {
						ViewModel.NotificationsEnabled = false;
					}
				});
				break;
			}
		});
	}
}