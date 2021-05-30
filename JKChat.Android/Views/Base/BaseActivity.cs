using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.ViewModels;

namespace JKChat.Android.Views.Base {
	public abstract class BaseActivity<TViewModel> : MvxActivity<TViewModel> where TViewModel : class, IMvxViewModel {
		public int LayoutId { get; private set; }
		public BaseActivity(int layoutId) {
			LayoutId = layoutId;
		}

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			var view = this.BindingInflate(LayoutId, null);
			SetContentView(view);
			var toolbar = view.FindViewById<Toolbar>(Resource.Id.toolbar);
			SetSupportActionBar(toolbar);
			SupportActionBar.SetDisplayShowHomeEnabled(true);
			SupportActionBar.SetDisplayHomeAsUpEnabled(true);
		}

		public override bool OnSupportNavigateUp() {
			OnBackPressed();
			return base.OnSupportNavigateUp();
		}
	}
}