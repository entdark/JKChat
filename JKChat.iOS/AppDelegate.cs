using Foundation;

using MvvmCross.Platforms.Ios.Core;

using UIKit;

namespace JKChat.iOS {
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register("AppDelegate")]
	public class AppDelegate : MvxSceneApplicationDelegate {
		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions) {
#if !DEBUG && !__MACCATALYST__
			AppCenter.Start(Core.ApiKeys.AppCenter.iOS, typeof(Crashes));
#endif
			return base.FinishedLaunching(application, launchOptions);
		}
	}
}