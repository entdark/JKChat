using JKChat.Android.Services;
using JKChat.Core;
using JKChat.Core.Services;

using MvvmCross;
using MvvmCross.Platforms.Android.Core;

namespace JKChat.Android {
	public class Setup : MvxAndroidSetup<App> {
		protected override void InitializeFirstChance() {
			Mvx.IoCProvider.RegisterSingleton<IDialogService>(() => new DialogService());
			base.InitializeFirstChance();
		}
	}
}