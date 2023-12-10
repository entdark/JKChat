using System;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.OS;
using Android.Runtime;

using JKChat.Android.Views.Main;
using JKChat.Core;
using JKChat.Core.Services;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;

using MvvmCross;
using MvvmCross.Core;
using MvvmCross.Platforms.Android.Core;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.ViewModels;

namespace JKChat.Android {
	[Application(
		//AllowBackup = false,
		Icon = "@mipmap/ic_launcher",
		RoundIcon = "@mipmap/ic_launcher_round",
		Label = "@string/app_name",
		Theme = "@style/AppThemeMaterial3",
		ResizeableActivity = true
	)]
	public class Application : MvxAndroidApplication<Setup, App> {
		public Application() {
		}

		public Application(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) {
		}

		public override void OnCreate() {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			AppCenter.Start(Core.ApiKeys.AppCenter.Android, typeof(Crashes));

			base.OnCreate();

			Mvx.IoCProvider.Resolve<IAppService>().AppTheme = AppSettings.AppTheme;

			RegisterActivityLifecycleCallbacks(new ActivityLifecycleCallbacks(this));
		}

		private class ActivityLifecycleCallbacks : Java.Lang.Object, IActivityLifecycleCallbacks {
			private readonly global::Android.App.Application application;
			private bool isResumed;
			private Bundle bundle;

			public ActivityLifecycleCallbacks(global::Android.App.Application application) {
				this.application = application;
			}

			private Setup setup => MvxAndroidSetupSingleton.EnsureSingletonAvailable(application).PlatformSetup<Setup>();

			public void OnActivityPaused(Activity activity) {
				if (!IsMainActivity(activity))
					return;
				isResumed = false;
				CancelMonitor();
			}
			public void OnActivityResumed(Activity activity) {
				if (!IsMainActivity(activity))
					return;
				isResumed = true;
				Monitor();
			}
			public void OnActivityCreated(Activity activity, Bundle savedInstanceState) {
				if (!IsMainActivity(activity))
					return;
				bundle = savedInstanceState;
				Monitor();
			}
			public void OnActivityDestroyed(Activity activity) {
				if (!IsMainActivity(activity))
					return;
				CancelMonitor();
			}
			public void OnActivitySaveInstanceState(Activity activity, Bundle outState) {}
			public void OnActivityStarted(Activity activity) {}
			public void OnActivityStopped(Activity activity) {}

			public void InitializationComplete() {
				if (!isResumed)
					return;
				if (Mvx.IoCProvider.TryResolve(out IMvxAppStart startup)) {
					if (!startup.IsStarted) {
						Task.Run(async () => await startup.StartAsync(bundle));
					}
				}
			}

			private void Monitor() {
				setup.StateChanged -= SetupStateChanged;
				if (setup.State == MvxSetupState.Initialized) {
					InitializationComplete();
				} else {
					setup.StateChanged += SetupStateChanged;
				}
			}

			private void CancelMonitor() {
				setup.StateChanged -= SetupStateChanged;
			}

			private void SetupStateChanged(object sender, MvxSetupStateEventArgs ev) {
				if (ev.SetupState == MvxSetupState.Initialized) {
					InitializationComplete();
				}
			}

			private static bool IsMainActivity(Activity activity) => activity is MainActivity;
		}
	}
}
