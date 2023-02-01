using Android.Graphics;
using Android.OS;
using Android.Views;

using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.Core.View;

using JKChat.Android.Controls.Toolbar;
using JKChat.Core.ViewModels.Base;

using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Platforms.Android.Views.Fragments;
using MvvmCross.ViewModels;

using static Android.Renderscripts.Sampler;

namespace JKChat.Android.Views.Base {
	public abstract class BaseFragment<TViewModel> : MvxFragment<TViewModel>, IBaseFragment where TViewModel : class, IMvxViewModel, IBaseViewModel {
		private BackDrawable backArrow;
		protected BackDrawable BackArrow {
			get => backArrow;
			set {
				backArrow = value;
				if (Toolbar != null) {
					Toolbar.NavigationIcon = value;
				}
			}
		}

		private string title;
		public string Title {
			get => title;
			set {
				title = value;
				SetTitle();
			}
		}

		public int Order { get; set; }

		protected ActionBar ActionBar => (Activity as MvxActivity)?.SupportActionBar;

		protected Toolbar ActivityToolbar => (Activity as IBaseActivity)?.Toolbar;

		protected Toolbar Toolbar { get; private set; }
		protected View ToolbarCustomView { get; private set; }

		protected IMenu Menu { get; private set; }

		protected int LayoutId { get; private set; }

		protected int MenuId { get; private set; }

		public BaseFragment(int layoutId, int menuId = int.MinValue) {
			LayoutId = layoutId;
			MenuId = menuId;
			if (menuId != int.MinValue) {
				HasOptionsMenu = true;
			}
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			this.EnsureBindingContextIsSet(inflater);
			return this.BindingInflate(LayoutId, null);
		}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			ViewCompat.SetTranslationZ(view, Order*0.1f);

			if (ActivityToolbar != null) {
				Toolbar = ActivityToolbar;
				BackArrow = Toolbar.NavigationIcon as BackDrawable;
			} else {
				Toolbar = view.FindViewById<Toolbar>(Resource.Id.toolbar);
				if (Toolbar != null) {
					Toolbar.NavigationClick += BackNavigationClick;
				}
				BackArrow = new BackDrawable() {
					Color = new Color(ContextCompat.GetColor(Context, Resource.Color.toolbar_menu)),
					RotatedColor = new Color(ContextCompat.GetColor(Context, Resource.Color.toolbar_menu)),
					StrokeWidth = 2.0f
				};
			}
			if (BackArrow != null) {
				BackArrow.AlwaysClose = false;
				//BackArrow.SetRotation(1.0f, false);
				//BackArrow.SetRotation(0.0f, true);
				BackArrow.SetRotation(0.0f, false);
			}

			SetUpNavigation(true);

			CreateOptionsMenu();

			var set = this.CreateBindingSet();
			set.Bind(this).For(v => v.Title).To(vm => vm.Title);
			set.Apply();
		}

		public override void OnDestroyView() {
			if (ActivityToolbar == null && Toolbar != null) {
				Toolbar.NavigationClick -= BackNavigationClick;
			}
			DestroyOptionsMenu();
			base.OnDestroyView();
		}

		public override void OnPause() {
			base.OnPause();
			ActivityPopEnter();
		}

		public override void OnResume() {
			base.OnResume();
			ActivityExit();
			DisplayCustomTitle(false);
		}

		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater) {
			inflater.Inflate(MenuId, menu);
			Menu = menu;
			CreateOptionsMenu();
			base.OnCreateOptionsMenu(menu, inflater);
		}

		public override void OnDestroyOptionsMenu() {
			Menu = null;
			base.OnDestroyOptionsMenu();
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

		protected virtual void ActivityExit() {
			if (Order == 1) {
				(Activity as IBaseActivity)?.Exit();
			}
		}

		protected virtual void ActivityPopEnter() {
			if (Order == 1) {
				(Activity as IBaseActivity)?.PopEnter();
			}
		}

		private bool toolbarCustomTitleAdded = false;
		protected virtual void SetCustomView(View view) {
			if (ActionBar != null) {
				ActionBar.CustomView = view;
			} else if (Toolbar != null) {
				if (ToolbarCustomView != null) {
					Toolbar.RemoveView(ToolbarCustomView);
				}
				if (view != null) {
					Toolbar.AddView(view);
					toolbarCustomTitleAdded = true;
				}
			}
			ToolbarCustomView = view;
		}

		protected virtual void DisplayCustomTitle(bool show) {
			if (ActionBar != null) {
				ActionBar.SetDisplayShowCustomEnabled(show);
				ActionBar.SetDisplayShowTitleEnabled(!show);
			} else if (Toolbar != null) {
				if (ToolbarCustomView != null) {
					if (show && !toolbarCustomTitleAdded) {
						Toolbar.AddView(ToolbarCustomView);
						toolbarCustomTitleAdded = true;
					} else if (!show && toolbarCustomTitleAdded) {
						Toolbar.RemoveView(ToolbarCustomView);
						toolbarCustomTitleAdded = false;
					}
				}
				Toolbar.Title = show ? null : Title;
			}
		}

		protected virtual void SetUpNavigation(bool showUpNavigation) {
			if (ActionBar != null) {
				ActionBar.SetDisplayHomeAsUpEnabled(showUpNavigation);
			} else if (Toolbar != null) {
				Toolbar.NavigationIcon = showUpNavigation ? BackArrow : null;
			}
		}

		protected virtual void CreateOptionsMenu() {
			if (ActionBar == null && Menu == null && MenuId != int.MinValue) {
				Toolbar?.InflateMenu(MenuId);
				Menu = Toolbar?.Menu;
			}
		}

		protected virtual void DestroyOptionsMenu() {
			if (ActionBar == null) {
				Menu = null;
			}
		}

		protected virtual void BackNavigationClick(object sender, Toolbar.NavigationClickEventArgs ev) {
			Activity?.OnBackPressed();
		}

		private void SetTitle() {
			if (ActionBar != null) {
				ActionBar.Title = Title;
			} else if (Toolbar != null && !toolbarCustomTitleAdded) {
				Toolbar.Title = Title;
			}
		}
	}
}