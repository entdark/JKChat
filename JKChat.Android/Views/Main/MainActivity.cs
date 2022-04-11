using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

using JKChat.Android.Controls;
using JKChat.Android.Helpers;
using JKChat.Android.Services;
using JKChat.Android.Views.Base;
using JKChat.Core.Messages;
using JKChat.Core.Models;
using JKChat.Core.Services;
using JKChat.Core.ViewModels.Main;

using MvvmCross;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Plugin.Messenger;

namespace JKChat.Android.Views.Main {
	[Activity(
		Label = "@string/app_name",
		Theme = "@style/AppTheme",
		MainLauncher = true,
		LaunchMode = LaunchMode.SingleTop,
		ConfigurationChanges = ConfigChanges.ScreenLayout | ConfigChanges.ScreenSize | ConfigChanges.SmallestScreenSize | ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
		WindowSoftInputMode = SoftInput.StateHidden
	)]
	[MvxActivityPresentation]
	public class MainActivity : BaseActivity<MainViewModel> {
		private MvxSubscriptionToken serverInfoMessageToken;
		private View contentPrimaryView, contentSecondaryView;

		public MainActivity() : base(Resource.Layout.activity_main) {}

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			if (savedInstanceState != null) {
				Mvx.IoCProvider.Resolve<IDialogService>().RestoreState();
			} else {
				Mvx.IoCProvider.Resolve<IDialogService>().Stop(true);
			}
			//if (savedInstanceState == null) {
				ViewModel.ShowInitialViewModelsCommand.Execute();
			//}
			if (serverInfoMessageToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
			}
			serverInfoMessageToken = Mvx.IoCProvider.Resolve<IMvxMessenger>().Subscribe<ServerInfoMessage>(OnServerInfoMessage);

			if (ViewPager != null) {
				ViewPager.OffscreenPageLimit = 2;
			}

			contentPrimaryView = FindViewById(Resource.Id.content_primary);
			contentSecondaryView = FindViewById(Resource.Id.content_secondary);
		}

		protected override void OnDestroy() {
			if (serverInfoMessageToken != null) {
				Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<ServerInfoMessage>(serverInfoMessageToken);
				serverInfoMessageToken = null;
			}
/*			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			gameClientsService.ShutdownAll();*/
//			Mvx.IoCProvider.Resolve<IDialogService>().Stop();
			base.OnDestroy();
		}

		protected override void OnStop() {
			base.OnStop();
		}

		protected override void OnStart() {
			base.OnStart();
		}

		protected override void OnResume() {
			base.OnResume();
		}

		protected override void OnSaveInstanceState(Bundle outState) {
			Mvx.IoCProvider.Resolve<IDialogService>().SaveState();
			base.OnSaveInstanceState(outState);
		}

		protected override void OnRestoreInstanceState(Bundle savedInstanceState) {
			base.OnRestoreInstanceState(savedInstanceState);
		}

		protected override void ConfigurationChanged(Configuration configuration) {
			base.ConfigurationChanged(configuration);
			const float primaryWidth = 480.0f;
			if (contentPrimaryView != null && contentPrimaryView.LayoutParameters is ViewGroup.MarginLayoutParams marginLayoutParameters) {
				marginLayoutParameters.Width = ExpandedWindow ? primaryWidth.DpToPx() : ViewGroup.LayoutParams.MatchParent;
				contentPrimaryView.LayoutParameters = marginLayoutParameters;
			}
			if (contentSecondaryView != null && contentSecondaryView.LayoutParameters is ViewGroup.MarginLayoutParams marginLayoutParameters2) {
				marginLayoutParameters2.LeftMargin = ExpandedWindow ? primaryWidth.DpToPx() : 0.0f.DpToPx();
				contentSecondaryView.LayoutParameters = marginLayoutParameters2;
			}
		}

		public override void Exit() {
			base.Exit();
			if (!ExpandedWindow) {
				var exitAnimation = AnimationUtils.LoadAnimation(this, Resource.Animation.fragment_hslide_rtl);
				contentPrimaryView.StartAnimation(exitAnimation);
			}
		}

		public override void PopEnter() {
			base.PopEnter();
			if (!ExpandedWindow) {
				var popEnterAnimation = AnimationUtils.LoadAnimation(this, Resource.Animation.fragment_hslide_ltr);
				contentPrimaryView.StartAnimation(popEnterAnimation);
			}
		}

		private void OnServerInfoMessage(ServerInfoMessage message) {
			var gameClientsService = Mvx.IoCProvider.Resolve<IGameClientsService>();
			int activeClients = gameClientsService.ActiveClients;
			if (activeClients > 0 && !IsServiceRunning(typeof(ForegroundGameClientsService))) {
				var intent = new Intent(this, typeof(ForegroundGameClientsService));
				StartService(intent);
			}
		}
		private bool IsServiceRunning(Type serviceClass) {
			ActivityManager manager = (ActivityManager)GetSystemService(Context.ActivityService);
			foreach (var service in manager.GetRunningServices(int.MaxValue)) {
				string className = service.Service.ClassName;
				if (className.Equals(serviceClass.Name, StringComparison.OrdinalIgnoreCase)
					|| (className.Contains(serviceClass.Name, StringComparison.OrdinalIgnoreCase)/* && className.Contains("md5")*/)) {
					return true;
				}
			}
			return false;
		}
	}
}