using System.Linq;

using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Views;

using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;

using JKChat.Android.Controls;
using JKChat.Android.Controls.Toolbar;
using JKChat.Android.Helpers;

using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.Platforms.Android.Views.ViewPager;
using MvvmCross.ViewModels;

namespace JKChat.Android.Views.Base {
	public abstract class BaseActivity<TViewModel> : MvxActivity<TViewModel>, IBaseActivity where TViewModel : class, IMvxViewModel {
		public bool ExpandedWindow { get; private set; }
		public Toolbar Toolbar { get; private set; }
		public ViewPager ViewPager { get; private set; }
		public int LayoutId { get; private set; }

		public BaseActivity(int layoutId) {
			LayoutId = layoutId;
		}

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			var view = this.BindingInflate(LayoutId, null);
			SetContentView(view);

			Toolbar = view.FindViewById<Toolbar>(Resource.Id.toolbar);
			if (Toolbar != null) {
				Toolbar.NavigationIcon = new BackDrawable() {
					Color = new Color(ContextCompat.GetColor(this, Resource.Color.toolbar_menu)),
					RotatedColor = new Color(ContextCompat.GetColor(this, Resource.Color.toolbar_menu)),
					StrokeWidth = 2.0f
				};
				SetSupportActionBar(Toolbar);
				SupportActionBar.SetDisplayShowHomeEnabled(true);
				SupportActionBar.SetDisplayHomeAsUpEnabled(true);
			}
			if (ViewPager == null) {
				ViewPager = view.FindViewById<ViewPager>(Resource.Id.content_viewpager);
			}

			var contentView = FindViewById<ViewGroup>(Resource.Id.content);
			contentView.AddView(new ConfigurationChangedView(this) {
				ConfigurationChanged = configuration => {
					ConfigurationChanged(configuration);
				}
			});
		}

		protected override void OnResume() {
			base.OnResume();
			ConfigurationChanged(null);
		}

		public override bool OnSupportNavigateUp() {
			OnBackPressed();
			return base.OnSupportNavigateUp();
		}

		public override void OnBackPressed() {
			IBaseFragment fragment;
			if (SupportFragmentManager?.BackStackEntryCount > 0) {
				fragment = SupportFragmentManager.Fragments?.LastOrDefault() as IBaseFragment;
			} else {
				fragment = Fragments?[ViewPager?.CurrentItem ?? 0] as IBaseFragment;
			}
			bool handled = fragment?.OnBackPressed() ?? false;
			if (!handled && SupportFragmentManager?.BackStackEntryCount <= 0 && ViewPager != null) {
				if (ViewPager.CurrentItem != 0) {
					ViewPager.SetCurrentItem(0, true);
					handled = true;
				}
			}
			if (!handled) {
				base.OnBackPressed();
			}
		}

		protected virtual void ConfigurationChanged(Configuration configuration) {
			const float maxWidth = 960.0f, maxHeight = 480.0f;
			var metrics = AndroidX.Window.Layout.WindowMetricsCalculator.Companion.OrCreate.ComputeCurrentWindowMetrics(this);
			//PxToDp doesn't work properly here for some reason
			float density = Resources?.DisplayMetrics?.Density ?? 1.0f,
				width = metrics.Bounds.Width() / density,
				height = metrics.Bounds.Height() / density;
			ExpandedWindow = width > maxWidth && height > maxHeight;
		}

		public virtual void Exit() {}

		public virtual void PopEnter() {}
	}
}