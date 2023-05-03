using System;
using System.Diagnostics;
using System.Text;

using UIKit;

namespace JKChat.iOS {
	public class Application {
		// This is the main entry point of the application.
		static void Main(string[] args) {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			// if you want to use a different Application Delegate class from "AppDelegate"
			// you can specify it here.
			try {
				UIApplication.Main(args, null, typeof(AppDelegate));
			} catch (Exception exception) {
				Microsoft.AppCenter.Crashes.Crashes.TrackError(exception);
				Debug.WriteLine(exception);
			}
		}
	}
}