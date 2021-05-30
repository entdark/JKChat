using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using JKChat.Core;
using JKChat.Core.Services;
using JKChat.iOS.Services;
using MvvmCross;
using MvvmCross.Platforms.Ios.Core;
using UIKit;

namespace JKChat.iOS {
	public class Setup : MvxIosSetup<App> {
		protected override void InitializeFirstChance() {
			Mvx.IoCProvider.RegisterSingleton<IDialogService>(() => new DialogService());
			base.InitializeFirstChance();
		}
	}
}