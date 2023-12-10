using System;
using Android;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
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
				}
			));
		}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			var recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			if (recyclerView.Adapter is not TableGroupedRecyclerViewAdapter)
				recyclerView.Adapter = new TableGroupedRecyclerViewAdapter((IMvxAndroidBindingContext)BindingContext);
			if (recyclerView.ItemTemplateSelector is not TableGroupedItemTemplateSelector)
				recyclerView.ItemTemplateSelector = new TableGroupedItemTemplateSelector(true);
			recyclerView.AddItemDecoration(new FadingMaterialDividerItemDecoration(Context, LinearLayoutManager.Vertical));

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
						var intent = new Intent(global::Android.Provider.Settings.ActionApplicationDetailsSettings, global::Android.Net.Uri.Parse($"package:" + global::Android.App.Application.Context.PackageName));
						try {
							StartActivity(intent);
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

		private class FadingMaterialDividerItemDecoration : MaterialDividerItemDecoration {
			private readonly Drawable dividerDrawable;
			private RecyclerView recyclerView;

			public FadingMaterialDividerItemDecoration(Context context, int orientation) : base(context, orientation) {
				try {
					var field = Class.Superclass.GetDeclaredField("dividerDrawable");
					field.Accessible = true;
					dividerDrawable = field.Get(this) as Drawable;
				} catch (Exception exception) {
					System.Diagnostics.Debug.WriteLine(exception);
				}
			}

			public override void OnDraw(Canvas canvas, RecyclerView parent, RecyclerView.State state) {
				recyclerView = parent;
				base.OnDraw(canvas, parent, state);
				recyclerView = null;
				dividerDrawable?.SetAlpha(255);
			}

			protected override bool ShouldDrawDivider(int position, RecyclerView.Adapter adapter) {
				bool shouldDraw = base.ShouldDrawDivider(position, adapter);
				if (shouldDraw && recyclerView != null && dividerDrawable != null) {
					var viewHolder = recyclerView?.FindViewHolderForAdapterPosition(position);
					var view = viewHolder?.ItemView;
					if (view != null) {
						dividerDrawable.Alpha = (int)(view.Alpha * 255);
					}
				}
				return shouldDraw;
			}
		}
	}
}