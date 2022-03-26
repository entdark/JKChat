using Android.OS;
using Android.Views;

using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;

using JKChat.Android.Controls.Toolbar;
using JKChat.Core.ViewModels.Base;

using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Platforms.Android.Views.Fragments;
using MvvmCross.ViewModels;

namespace JKChat.Android.Views.Base {
	public abstract class BaseFragment<TViewModel> : MvxFragment<TViewModel>, IBaseFragment where TViewModel : class, IMvxViewModel, IBaseViewModel {
		protected BackDrawable BackArrow {
			get => Toolbar?.NavigationIcon as BackDrawable;
			set {
				if (Toolbar != null) {
					Toolbar.NavigationIcon = value;
				}
			}
		}

		public string Title {
			get => ActionBar?.Title;
			set {
				if (ActionBar != null) {
					ActionBar.Title = value;
				}
			}
		}

		public ActionBar ActionBar => (Activity as MvxActivity)?.SupportActionBar;

		public Toolbar Toolbar => (Activity as IBaseActivity)?.Toolbar;

		public int LayoutId { get; private set; }

		public BaseFragment(int layoutId) {
			LayoutId = layoutId;
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

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);
			BackArrow.AlwaysClose = false;
			BackArrow.SetRotation(1.0f, false);
			BackArrow.SetRotation(0.0f, true);
		}

		public override void OnDestroyView() {
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

		public virtual bool OnBackPressed() {
			return false;
		}
	}
}