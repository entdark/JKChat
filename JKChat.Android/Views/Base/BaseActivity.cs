using System;
using System.Collections.Generic;
using System.Linq;

using Android.Animation;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.Animations;

using AndroidX.AppCompat.Widget;
using AndroidX.Core.View;

using Google.Android.Material.Color;

using JKChat.Android.Controls;
using JKChat.Android.Controls.Toolbar;
using JKChat.Android.Helpers;
using JKChat.Core;

using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views;
using MvvmCross.ViewModels;

namespace JKChat.Android.Views.Base {
	public abstract class BaseActivity<TViewModel> : MvxActivity<TViewModel>, IBaseActivity where TViewModel : class, IMvxViewModel {
		public bool ExpandedWindow { get; private set; }
		public Toolbar Toolbar { get; private set; }
		public int LayoutId { get; private set; }

		public BaseActivity(int layoutId) {
			LayoutId = layoutId;
		}

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			var view = this.BindingInflate(LayoutId, null);
			SetContentView(view);
			ViewCompat.SetOnApplyWindowInsetsListener(view, new OnApplyWindowInsetsListener());
//TODO: try on Android 11 or above - laggy on Android 10
//			ViewCompat.SetWindowInsetsAnimationCallback(view, new WindowInsetsAnimationCallback(WindowInsetsAnimationCompat.Callback.DispatchModeStop, view));

			Toolbar = view.FindViewById<Toolbar>(Resource.Id.toolbar);
			if (Toolbar != null) {
				Toolbar.NavigationIcon = new BackDrawable() {
					Color = new Color(MaterialColors.GetColor(this, Resource.Attribute.colorOnSurface, Color.Transparent)),
					RotatedColor = new Color(MaterialColors.GetColor(this, Resource.Attribute.colorOnSurface, Color.Transparent)),
					StrokeWidth = 2.0f
				};
				SetSupportActionBar(Toolbar);
				SupportActionBar.SetDisplayShowHomeEnabled(true);
				SupportActionBar.SetDisplayHomeAsUpEnabled(true);
			}

			var contentView = FindViewById<ViewGroup>(Resource.Id.content);
			contentView.AddView(new ConfigurationChangedView(this) {
				ConfigurationChanged = ConfigurationChanged
			});
		}

		protected override void OnDestroy() {
			App.AllowReset = this.IsFinishing;
			base.OnDestroy();
			App.AllowReset = true;
		}

		protected override void OnResume() {
			base.OnResume();
			ConfigurationChanged(null);
		}

		public override bool OnSupportNavigateUp() {
			OnBackPressedDispatcher.OnBackPressed();
			return base.OnSupportNavigateUp();
		}

		protected virtual void ConfigurationChanged(Configuration configuration) {
#if DEBUG
			const float maxWidth = 640.0f, maxHeight = 320.0f;
#else
			const float maxWidth = 960.0f, maxHeight = 480.0f;
#endif
			var metrics = AndroidX.Window.Layout.WindowMetricsCalculator.Companion.OrCreate.ComputeCurrentWindowMetrics(this);
			//PxToDp doesn't work properly here for some reason
			float density = Resources?.DisplayMetrics?.Density ?? 1.0f,
				width = metrics.Bounds.Width().PxToDp() / density,
				height = metrics.Bounds.Height() / density;
			ExpandedWindow = width > maxWidth && height > maxHeight;
		}

		public virtual void Exit() {}

		public virtual void PopEnter() {}

		private class OnApplyWindowInsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener {
			public WindowInsetsCompat OnApplyWindowInsets(View view, WindowInsetsCompat insets) {
				int bottom = insets.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom
					- insets.GetInsets(WindowInsetsCompat.Type.SystemBars()).Bottom;
				view.SetPadding(0, 0, 0, Math.Max(bottom, 0));
				return insets;
			}
		}

		private class WindowInsetsAnimationCallback : WindowInsetsAnimationCompat.Callback {
			private readonly View view;

			private int start, end, ddy;
			private bool imeShowing = false;
			private ValueAnimator animator;

			public WindowInsetsAnimationCallback(int dispatchMode, View view) : base(dispatchMode) {
				this.view = view;
			}

			public override void OnPrepare(WindowInsetsAnimationCompat animation) {
				System.Diagnostics.Debug.WriteLine(nameof(OnPrepare));
				base.OnPrepare(animation);
				start = view.PaddingBottom;
				end = (ViewCompat.GetRootWindowInsets(view)?.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom ?? 0)
					- (ViewCompat.GetRootWindowInsets(view)?.GetInsets(WindowInsetsCompat.Type.SystemBars()).Bottom ?? 0)*2;
				end = Math.Max(end, 0);
				ddy = end - start;
				imeShowing = !ViewCompat.GetRootWindowInsets(view).IsVisible(WindowInsetsCompat.Type.Ime());
				System.Diagnostics.Debug.WriteLine($"ime'v: {ViewCompat.GetRootWindowInsets(view).GetInsets(WindowInsetsCompat.Type.Ime()).Bottom}, system'v: {ViewCompat.GetRootWindowInsets(view).GetInsets(WindowInsetsCompat.Type.SystemBars()).Bottom}");
			}

			public override WindowInsetsAnimationCompat.BoundsCompat OnStart(WindowInsetsAnimationCompat animation, WindowInsetsAnimationCompat.BoundsCompat bounds) {
				System.Diagnostics.Debug.WriteLine(nameof(OnStart));
				if ((animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0) {
					var animator = ValueAnimator.OfInt(view.PaddingBottom, end);
					animator.SetDuration(animation.DurationMillis);
					animator.SetInterpolator(animation.Interpolator);
					animator.Update += AnimatorUpdate;
					animator.AnimationEnd += AnimatorAnimationEnd;
					animator.Start();
/*					view
						.Animate()
						.TranslationY(-bounds.UpperBound.Bottom)
						.SetDuration(animation.DurationMillis)
						.SetInterpolator(animation.Interpolator)
						.Start();*/
					end = bounds.UpperBound.Bottom;
				}
				return base.OnStart(animation, bounds);
			}

			private void AnimatorAnimationEnd(object sender, EventArgs ev) {
				var animator = sender as ValueAnimator;
				animator.Update -= AnimatorUpdate;
				animator.AnimationEnd -= AnimatorAnimationEnd;
			}

			private void AnimatorUpdate(object sender, ValueAnimator.AnimatorUpdateEventArgs ev) {
				view.SetPadding(0, 0, 0, (ev.Animation.AnimatedValue as Java.Lang.Integer)?.IntValue() ?? 0);
			}

			public override WindowInsetsCompat OnProgress(WindowInsetsCompat insets, IList<WindowInsetsAnimationCompat> runningAnimations) {
				if (runningAnimations.FirstOrDefault(animation => (animation.TypeMask & WindowInsetsCompat.Type.Ime()) != 0) is {} imeAnimation) {
					int bottom = insets.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom;
//					if (!imeShowing)
//						bottom -= insets.GetInsets(WindowInsetsCompat.Type.SystemBars()).Bottom;
					float dy = start + ddy * imeAnimation.InterpolatedFraction;
					System.Diagnostics.Debug.WriteLine($"dy: {dy}");
					System.Diagnostics.Debug.WriteLine($"ime'v: {ViewCompat.GetRootWindowInsets(view).GetInsets(WindowInsetsCompat.Type.Ime()).Bottom}, system'v: {ViewCompat.GetRootWindowInsets(view).GetInsets(WindowInsetsCompat.Type.SystemBars()).Bottom}");
					System.Diagnostics.Debug.WriteLine($"ime'i: {insets.GetInsets(WindowInsetsCompat.Type.Ime()).Bottom}, system'i: {insets.GetInsets(WindowInsetsCompat.Type.SystemBars()).Bottom}");
//					view.SetPadding(0, 0, 0, (int)dy);
//					view.TranslationY = dy;
				}
				return insets;
			}

			public override void OnEnd(WindowInsetsAnimationCompat animation) {
				System.Diagnostics.Debug.WriteLine(nameof(OnEnd));
				System.Diagnostics.Debug.WriteLine($"ime'v: {ViewCompat.GetRootWindowInsets(view).GetInsets(WindowInsetsCompat.Type.Ime()).Bottom}, system'v: {ViewCompat.GetRootWindowInsets(view).GetInsets(WindowInsetsCompat.Type.SystemBars()).Bottom}");
				base.OnEnd(animation);
			}
		}
	}
}