using System;
using System.Globalization;

using Android.Graphics.Drawables;

using AndroidX.Core.Content;

using JKChat.Core.Models;

using MvvmCross.Converters;

using Microsoft.Maui.ApplicationModel;

namespace JKChat.Android.ValueConverters {
	public class PlayerTeamBackgroundValueConverter : MvxValueConverter<Team, Drawable> {
		protected override Drawable Convert(Team value, Type targetType, object parameter, CultureInfo culture) {
			return value switch {
				Team.Free => new ColorDrawable(new(ContextCompat.GetColor(Platform.CurrentActivity, Resource.Color.team_free))),
				Team.Red => new ColorDrawable(new(ContextCompat.GetColor(Platform.CurrentActivity, Resource.Color.team_red))),
				Team.Blue => new ColorDrawable(new(ContextCompat.GetColor(Platform.CurrentActivity, Resource.Color.team_blue))),
				Team.Spectator => null,
				_ => null
			};
		}
	}
}

