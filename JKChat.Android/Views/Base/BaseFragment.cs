using System;

using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.Animations;

using AndroidX.AppCompat.App;
using AndroidX.AppCompat.Widget;

using Google.Android.Material.Color;

using Java.Util.Concurrent;

using JKChat.Android.Callbacks;
using JKChat.Android.Controls.Toolbar;
using JKChat.Android.Helpers;
using JKChat.Core.Navigation;
using JKChat.Core.ViewModels.Base;

using MvvmCross;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Platforms.Android.Views.Fragments;

namespace JKChat.Android.Views.Base {
	public abstract class BaseFragment<TViewModel> : MvxFragment<TViewModel>, IBaseFragment where TViewModel : class, IBaseViewModel {
		private const string bundleOrder = nameof(BaseFragment<TViewModel>) + nameof(bundleOrder);
		private const string bundleRegisterBackPressedCallback = nameof(BaseFragment<TViewModel>) + nameof(bundleRegisterBackPressedCallback);

		private OnBackPressedCallback onBackPressedCallback;

		protected ActionBar ActionBar => (Activity as MvxActivity)?.SupportActionBar;

		protected Toolbar ActivityToolbar => BaseActivity?.Toolbar;

		protected IBaseActivity BaseActivity => Activity as IBaseActivity;

		protected Toolbar Toolbar { get; private set; }
		protected View ToolbarCustomTitleView { get; private set; }

		protected IMenu Menu { get; private set; }

		protected int LayoutId { get; init; }

		protected int MenuId { get; init; }

		public int Order { get; set; }
		public bool RegisterBackPressedCallback { get; set; }
		public bool PostponeTransition { get; init; }

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

			SetNavigationBarContrastEnforced(true);
			
			if (savedInstanceState != null)
				OnRestoreInstanceState(savedInstanceState);

			if (PostponeTransition) {
				PostponeEnterTransition(2, TimeUnit.Milliseconds);
			}

			if (ActivityToolbar != null) {
				Toolbar = ActivityToolbar;
				BackArrow = Toolbar.NavigationIcon as BackDrawable;
			} else {
				Toolbar = view.FindViewById<Toolbar>(Resource.Id.toolbar);
				if (Toolbar != null) {
					Toolbar.NavigationClick += BackNavigationClick;
				}
				BackArrow = new BackDrawable() {
					Color = new Color(MaterialColors.GetColor(Context, Resource.Attribute.colorOnSurface, Color.Transparent)),
					RotatedColor = new Color(MaterialColors.GetColor(Context, Resource.Attribute.colorOnSurface, Color.Transparent)),
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

			if (RegisterBackPressedCallback) {
				onBackPressedCallback?.Remove();
				onBackPressedCallback = new OnBackPressedCallback(OnBackPressedCallback);
				Activity?.OnBackPressedDispatcher.AddCallback(this, onBackPressedCallback);
			}

			using var set = this.CreateBindingSet();
			BindTitle(set);

			if (BaseActivity != null) {
				OnConfigurationChanged(BaseActivity.LayoutState, BaseActivity.Landscape);
				BaseActivity.ConfigurationChanged += OnConfigurationChanged;
			}
		}

		public override void OnDestroyView() {
			if (ActivityToolbar == null && Toolbar != null) {
				Toolbar.NavigationClick -= BackNavigationClick;
			}
			DestroyOptionsMenu();
			onBackPressedCallback?.Remove();

			if (BaseActivity != null) {
				BaseActivity.ConfigurationChanged -= OnConfigurationChanged;
			}

			base.OnDestroyView();
		}

		public override void OnPause() {
			base.OnPause();
			HideKeyboard();
			ActivityPopEnter();
		}

		public override void OnResume() {
			base.OnResume();
			ActivityExit();
			DisplayCustomTitle(false);
		}

		[Obsolete]
		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater) {
			inflater.Inflate(MenuId, menu);
			Menu = menu;
			CreateOptionsMenu();
			base.OnCreateOptionsMenu(menu, inflater);
		}

		[Obsolete]
		public override void OnDestroyOptionsMenu() {
			Menu = null;
			base.OnDestroyOptionsMenu();
		}

		public override void OnSaveInstanceState(Bundle outState) {
			base.OnSaveInstanceState(outState);
			outState.PutInt(bundleOrder, Order);
			outState.PutBoolean(bundleRegisterBackPressedCallback, RegisterBackPressedCallback);
		}

		public virtual void OnRestoreInstanceState(Bundle savedInstanceState) {
			Order = savedInstanceState.GetInt(bundleOrder, Order);
			RegisterBackPressedCallback = savedInstanceState.GetBoolean(bundleRegisterBackPressedCallback, RegisterBackPressedCallback);
		}

		public override Animation OnCreateAnimation(int transit, bool enter, int nextAnim) {
			if (IBaseFragment.DisableAnimations)
				return new NullAnimation();
			return base.OnCreateAnimation(transit, enter, nextAnim);
		}

		public virtual bool OnBackPressed() {
			return false;
		}

		protected virtual void BindTitle(MvxFluentBindingDescriptionSet<IMvxFragmentView<TViewModel>, TViewModel> set) {
			set.Bind(this).For(v => v.Title).To(vm => vm.Title);
		}

		protected virtual void OnBackPressedCallback() {
			if (ParentFragmentManager is { IsStateSaved: false } && ParentFragmentManager.PopBackStackImmediate())
				return;

			Mvx.IoCProvider.Resolve<INavigationService>().Close(ViewModel);
		}

		protected void ToggleBackPressedCallback(bool enable) {
			if (onBackPressedCallback != null)
				onBackPressedCallback.Enabled = enable;
		}

		protected virtual void ActivityExit() {
			if (Order == 1) {
				HideKeyboard();
			}
			BaseActivity?.Exit(Order);
		}

		protected virtual void ActivityPopEnter() {
			if (Order == 1) {
				HideKeyboard();
			}
			BaseActivity?.PopEnter(Order);
		}

		protected virtual void SetNavigationBarContrastEnforced(bool value) {
			if (Order < 1)
				return;
			BaseActivity?.SetNavigationBarContrastEnforced(value);
		}

		protected virtual void OnConfigurationChanged(LayoutState layoutState, bool landscape) {}

		private bool toolbarCustomTitleAdded = false;
		protected virtual void SetCustomTitleView(View view) {
			if (ActionBar != null) {
				ActionBar.CustomView = view;
			} else if (Toolbar != null) {
				if (ToolbarCustomTitleView != null) {
					Toolbar.RemoveView(ToolbarCustomTitleView);
				}
				if (view != null) {
					Toolbar.AddView(view);
					toolbarCustomTitleAdded = true;
				}
			}
			ToolbarCustomTitleView = view;
		}

		protected virtual void DisplayCustomTitle(bool show) {
			if (ActionBar != null) {
				ActionBar.SetDisplayShowCustomEnabled(show);
				ActionBar.SetDisplayShowTitleEnabled(!show);
			} else if (Toolbar != null) {
				if (ToolbarCustomTitleView != null) {
					if (show && !toolbarCustomTitleAdded) {
						Toolbar.AddView(ToolbarCustomTitleView);
						toolbarCustomTitleAdded = true;
					} else if (!show && toolbarCustomTitleAdded) {
						Toolbar.RemoveView(ToolbarCustomTitleView);
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
			Activity?.OnBackPressedDispatcher.OnBackPressed();
		}

		protected void ShowKeyboard(View view = null) {
			Context.ShowKeyboard(view);
		}
		protected void HideKeyboard(View view = null, bool clearFocus = false) {
			Context.HideKeyboard(view, clearFocus);
		}

		private void SetTitle() {
			if (ActionBar != null) {
				ActionBar.Title = Title;
			} else if (Toolbar != null && !toolbarCustomTitleAdded) {
				Toolbar.Title = Title;
			}
		}

		private class NullAnimation : Animation {
			public NullAnimation() {
				Duration = 0L;
			}
		}
	}
}