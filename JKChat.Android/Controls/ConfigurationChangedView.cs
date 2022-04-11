using System;

using Android.Content;
using Android.Content.Res;
using Android.Views;

namespace JKChat.Android.Controls {
	public class ConfigurationChangedView : View {
		public Action<Configuration> ConfigurationChanged { get; set; }

		public ConfigurationChangedView(Context context) : base(context) {
		}

		protected override void OnConfigurationChanged(Configuration newConfig) {
			base.OnConfigurationChanged(newConfig);
			ConfigurationChanged?.Invoke(newConfig);
		}
	}
}