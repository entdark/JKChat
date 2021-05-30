using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using JKChat.Core.ViewModels.Base;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Platforms.Android.Views.Fragments;
using MvvmCross.ViewModels;

namespace JKChat.Android.Views.Base {
	public abstract class BaseFragment<TViewModel> : MvxFragment<TViewModel> where TViewModel : class, IMvxViewModel, IBaseViewModel {
		private string title;
		public string Title {
			get => title;
			set => SetToolbarTitle(title = value);
		}

		public ActionBar ActionBar {
			get => (Activity as MvxActivity)?.SupportActionBar;
		}


		public int LayoutId { get; private set; }

		public BaseFragment(int layoutId) {
			LayoutId = layoutId;
		}

		public override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			// Create your fragment here
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			ActionBar?.SetDisplayHomeAsUpEnabled(true);
			this.EnsureBindingContextIsSet(inflater);
			var view = this.BindingInflate(LayoutId, null);
			var set = this.CreateBindingSet();
			set.Bind(this).For(v => v.Title).To(vm => vm.Title);
			set.Apply();
			return view;
		}

		public override void OnDestroyView() {
//			ActionBar?.SetDisplayHomeAsUpEnabled(false);
			base.OnDestroyView();
		}

		protected void SetToolbarTitle(string title) {
			if (ActionBar != null) {
				ActionBar.Title = title;
			}
		}

		public override void OnResume() {
			base.OnResume();
			DisplayCustomTitle();
		}

		protected virtual void DisplayCustomTitle() {
			ActionBar?.SetDisplayShowCustomEnabled(false);
			ActionBar?.SetDisplayShowTitleEnabled(true);
		}

		public override void OnSaveInstanceState(Bundle outState) {
			base.OnSaveInstanceState(outState);
		}

		public override void OnViewStateRestored(Bundle savedInstanceState) {
			base.OnViewStateRestored(savedInstanceState);
		}
	}
}