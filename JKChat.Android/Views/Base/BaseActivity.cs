using System.Linq;

using Android.Graphics;
using Android.OS;

using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;

using JKChat.Android.Controls.Toolbar;

using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.ViewModels;

namespace JKChat.Android.Views.Base {
	public abstract class BaseActivity<TViewModel> : MvxActivity<TViewModel>, IBaseActivity where TViewModel : class, IMvxViewModel {
		public Toolbar Toolbar { get; private set; }
		public int LayoutId { get; private set; }
		public BaseActivity(int layoutId) {
			LayoutId = layoutId;
		}

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			var view = this.BindingInflate(LayoutId, null);
			SetContentView(view);

			Toolbar = view.FindViewById<Toolbar>(Resource.Id.toolbar);
			Toolbar.NavigationIcon = new BackDrawable() {
				Color = new Color(ContextCompat.GetColor(this, Resource.Color.toolbar_menu)),
				RotatedColor = new Color(ContextCompat.GetColor(this, Resource.Color.toolbar_menu)),
				StrokeWidth = 2.0f
			};
			SetSupportActionBar(Toolbar);
			SupportActionBar.SetDisplayShowHomeEnabled(true);
			SupportActionBar.SetDisplayHomeAsUpEnabled(true);
		}

		public override bool OnSupportNavigateUp() {
			OnBackPressed();
			return base.OnSupportNavigateUp();
		}

		public override void OnBackPressed() {
			var fragment = Fragments.LastOrDefault();
			bool handled = false;
			if (fragment is IBaseFragment baseFragment) {
				handled = baseFragment.OnBackPressed();
			}
			if (!handled) {
				base.OnBackPressed();
			}
		}
	}
}