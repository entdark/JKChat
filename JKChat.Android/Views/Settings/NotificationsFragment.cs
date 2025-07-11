﻿using System;

using Android;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;

using Google.Android.Material.Divider;

using JKChat.Android.Adapters;
using JKChat.Android.Callbacks;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Services;
using JKChat.Android.TemplateSelectors;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Settings;

using Microsoft.Maui.ApplicationModel;

using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;

namespace JKChat.Android.Views.Settings {
	[PushFragmentPresentation]
	public class NotificationsFragment : BaseFragment<NotificationsViewModel> {
		private ActivityResultLauncher notificationsPermissionActivityResultLauncher;

		private bool notificationsEnabled;
		public bool NotificationsEnabled {
			get => notificationsEnabled;
			set {
				notificationsEnabled = value;
				CheckNotificationsPermission(notificationsEnabled);
			}
		}

		public NotificationsFragment() : base(Resource.Layout.notifications_page) {
			PostponeTransition = true;
		}

		public override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			notificationsPermissionActivityResultLauncher = RegisterForActivityResult(
				new ActivityResultContracts.RequestPermission(),
				new ActivityResultCallback<Java.Lang.Boolean>(granted => {
					CheckNotificationsPermission(granted.BooleanValue());
				})
			);
		}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			var recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			if (recyclerView.Adapter is not TableGroupedRecyclerViewAdapter)
				recyclerView.Adapter = new TableGroupedRecyclerViewAdapter((IMvxAndroidBindingContext)BindingContext);
			if (recyclerView.ItemTemplateSelector is not TableGroupedItemTemplateSelector)
				recyclerView.ItemTemplateSelector = new TableGroupedItemTemplateSelector(true);
			recyclerView.AddItemDecoration(new MaterialDividerItemDecoration(Context, LinearLayoutManager.Vertical));

			using var set = this.CreateBindingSet();
			set.Bind(this).For(v => v.NotificationsEnabled).To(vm => vm.NotificationsEnabled);
		}

		private void CheckNotificationsPermission(bool enabled) {
			if (!enabled)
				return;
			bool isTiramisuOrHigher = Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu;
			var permission = ContextCompat.CheckSelfPermission(Context, Manifest.Permission.PostNotifications);
			if (permission == Permission.Granted) {
				//we are good
			} else if (!isTiramisuOrHigher || (isTiramisuOrHigher && ShouldShowRequestPermissionRationale(Manifest.Permission.PostNotifications))) {
				DialogService.Show(new() {
					Title = "Notifications disabled",
					Message = "Go to application settings to enable notifications",
					OkText = "Open Settings",
					CancelText = "Cancel",
					OkAction = _ => {
						ViewModel.NotificationsEnabled = false;
						try {
							AppInfo.ShowSettingsUI();
						} catch (Exception exception) {
							System.Diagnostics.Debug.WriteLine(exception);
						}
					},
					CancelAction = _ => {
						ViewModel.NotificationsEnabled = false;
					}
				});
			} else if (isTiramisuOrHigher) {
				notificationsPermissionActivityResultLauncher.Launch(new Java.Lang.String(Manifest.Permission.PostNotifications));
			}
		}
	}
}