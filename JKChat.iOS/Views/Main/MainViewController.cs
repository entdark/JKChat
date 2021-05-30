using System;
using System.Drawing;

using CoreFoundation;
using UIKit;
using Foundation;
using MvvmCross.Platforms.Ios.Views;
using JKChat.Core.ViewModels.Main;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using JKChat.Core.ViewModels.ServerList;
using JKChat.iOS.Views.Base;

namespace JKChat.iOS.Views.Main {
	public class MainViewController : MvxViewController<MainViewModel> {
		private bool firstAppearance = false;
		public MainViewController() {
		}


		public override void ViewWillAppear(bool animated) {
			base.ViewWillAppear(animated);
			if (!firstAppearance) {
				firstAppearance = true;
				ViewModel.ShowServerListCommand.Execute();
			}
		}
	}
}