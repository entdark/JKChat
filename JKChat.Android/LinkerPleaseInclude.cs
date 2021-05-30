using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Widget;

using AndroidX.RecyclerView.Widget;

using MvvmCross.Commands;
using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Plugin.Visibility;

namespace JKChat.Android {
	public class LinkerPleaseInclude {
		public void Include(MvxVisibilityValueConverter vvc) {
			vvc = new MvxVisibilityValueConverter();
		}
		public void Include(MvxInvertedVisibilityValueConverter ivvc) {
			ivvc = new MvxInvertedVisibilityValueConverter();
		}
		public void Include(RecyclerView.ViewHolder vh, MvxRecyclerView list) {
			vh.ItemView.Click += (sender, ev) => { list.ItemsSource = null; };
			vh.ItemView.LongClick += (sender, ev) => { list.ItemsSource = null; };
			list.Click += (sender, ev) => { list.ItemsSource = null; };
		}
		public void Include(TextView textView) {
			textView.AfterTextChanged += (sender, args) => { textView.Text = string.Empty; };
			textView.Hint = string.Empty;
		}
	}
}