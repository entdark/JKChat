using System;

using Android.Content;
using Android.Content.Res;
using Android.Views;

namespace JKChat.Android.Controls {
	public class ConfigurationChangedView(Context context) : View(context)
	{
		public Action<Configuration> ConfigurationChanged { get; set; }

		protected override void OnConfigurationChanged(Configuration newConfig) {
			base.OnConfigurationChanged(newConfig);
			ConfigurationChanged?.Invoke(newConfig);
		}
	}
}