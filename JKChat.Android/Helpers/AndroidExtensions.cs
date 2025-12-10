using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.InputMethodServices;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;

using AndroidX.Core.Content;
using AndroidX.Core.Util;
using AndroidX.Core.View;

using Google.Android.Material.Button;
using Google.Android.Material.Color;
using Google.Android.Material.Internal;
using JKChat.Android.Controls.Listeners;
using JKChat.Android.Controls.Toolbar;

using Microsoft.Maui.ApplicationModel;

namespace JKChat.Android.Helpers {
	public static class AndroidExtensions {
		public static int DpToPx(this int dp) {
			return (int)Math.Ceiling(TypedValueCompat.DpToPx(dp, Context?.Resources?.DisplayMetrics));
		}
		public static int DpToPx(this float dp) {
			return (int)Math.Ceiling(TypedValueCompat.DpToPx(dp, Context?.Resources?.DisplayMetrics));
		}
		public static float DpToPxF(this float dp) {
			return TypedValueCompat.DpToPx(dp, Context?.Resources?.DisplayMetrics);
		}
		public static float PxToDp(this int px) {
			return TypedValueCompat.PxToDp(px, Context?.Resources?.DisplayMetrics);
		}
		public static float PxToDp(this float px) {
			return TypedValueCompat.PxToDp(px, Context?.Resources?.DisplayMetrics);
		}
		public static float SpToPxF(this float sp) {
			return TypedValueCompat.SpToPx(sp, Context?.Resources?.DisplayMetrics);
		}

		public static int GetDimensionInPx(this Context context, int id) {
			return (int)(context?.Resources?.GetDimension(id) ?? 0.0f);
		}

		public static float GetDimensionInPxF(this Context context, int id) {
			return context?.Resources?.GetDimension(id) ?? 0.0f;
		}

		public static float GetDimensionInDp(this Context context, int id) {
			return (context?.Resources?.GetDimension(id) ?? 0.0f).PxToDp();
		}

		public static void SetClickAction(this IMenuItem item, Action action) {
			if (item?.ActionView is FadingImageView imageView || (imageView = item?.ActionView?.FindViewById<FadingImageView>(Resource.Id.toolbar_menu_item)) != null) {
				imageView.Action = action;
			} else {
				item?.SetOnMenuItemClickListener(new MenuItemClickListener() {
					Click = () => {
						action?.Invoke();
						return true;
					}
				});
			}
		}
		public static void AdjustIconInsets(this IMenuItem item) {
			if (item.Icon != null && item.Icon is not InsetDrawable)
				item.SetIcon(new InsetDrawable(item.Icon, 4.0f.DpToPx(), 0, 4.0f.DpToPx(), 0));
		}
		public static void SetVisible(this IMenuItem item, bool visible, bool animated) {
			if (visible || !animated) {
				item?.SetVisible(visible);
			}
			if (item?.ActionView is FadingImageView imageView || (imageView = item?.ActionView?.FindViewById<FadingImageView>(Resource.Id.toolbar_menu_item)) != null) {
				imageView.HideShow(visible, animated, () => {
					if (!visible && animated) {
						item?.SetVisible(visible);
					}
				});
			} 
		}
		public static void ToggleIconButton(this MaterialButton button, int iconId, bool show) {
			var context = button.Context;
			if (show && button.Icon == null) {
				button.Icon = ContextCompat.GetDrawable(context, iconId);
				button.SetPadding(context.GetDimensionInPx(Resource.Dimension.m3_btn_icon_btn_padding_left), button.PaddingTop, context.GetDimensionInPx(Resource.Dimension.m3_btn_icon_btn_padding_right), button.PaddingBottom);
			} else if (!show && button.Icon != null) {
				button.Icon = null;
				button.SetPadding(context.GetDimensionInPx(Resource.Dimension.m3_btn_padding_left), button.PaddingTop, context.GetDimensionInPx(Resource.Dimension.m3_btn_padding_right), button.PaddingBottom);
			}
		}
		public static void ShowKeyboard(this Context context, View view = null) {
			if ((view != null && (view.IsFocused || view.RequestFocus())) || view == null) {
				var imm = (InputMethodManager)context.GetSystemService(InputMethodService.InputMethodService);
				imm.ShowSoftInput(view, ShowFlags.Implicit);
			}
		}
		public static void HideKeyboard(this Context context, View view = null, bool clearFocus = false) {
			view = (context as Activity)?.CurrentFocus ?? view;
			if (view != null) {
				var imm = (InputMethodManager)context.GetSystemService(InputMethodService.InputMethodService);
				imm.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.ImplicitOnly);
				if (clearFocus)
					view.ClearFocus();
			}
		}
		public static void HideKeyboard(this Context context, bool clearFocus) {
			context.HideKeyboard(null, clearFocus);
		}
		public static IDictionary<string, string> ToDictionary(this Bundle bundle) {
			return bundle?.IsEmpty ?? true ? new Dictionary<string, string>() : bundle.KeySet().ToDictionary(key => key, bundle.GetString);
		}
		public static bool IsNavigationBarOnLeft(this WindowInsetsCompat windowInsets) {
			var insets = windowInsets?.GetInsets(WindowInsetsCompat.Type.SystemBars());
			return insets is { Left: > 0 };
		}
		public static bool IsNavigationBarOnRight(this WindowInsetsCompat windowInsets) {
			var insets = windowInsets?.GetInsets(WindowInsetsCompat.Type.SystemBars());
			return insets is { Right: > 0 };
		}

		public static ViewGroup AddView(this ViewGroup vg, Func<Context, View> viewCreator) {
			return vg.AddView(new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent), viewCreator);
		}
		public static ViewGroup AddView(this ViewGroup vg, ViewGroup.LayoutParams parameters, Func<Context, View> viewCreator) {
			parameters ??= new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			vg.AddView(viewCreator(vg.Context), parameters);
			return vg;
		}
		public static ViewGroup AddView(this ViewGroup vg, int width = -1, int height = -1, Func<Context, View> viewCreator = null) {
			return vg.AddView(new ViewGroup.LayoutParams(width, height), viewCreator);
		}
		public static T Adjust<T>(this T obj, Action<T> adjust) {
			adjust(obj);
			return obj;
		}
		public static void SetAttributeBackgroundColor(this View view, int colorAttributeResId) {
			var drawable = new ColorDrawable(new(MaterialColors.GetColor(view.Context, colorAttributeResId, Color.Transparent)));
			view.Background = drawable;
		}
		public static void SetAttributeBackgroundResource(this View view, int attributeResId) {
			using var typedValue = new TypedValue();
			view.Context.Theme.ResolveAttribute(attributeResId, typedValue, true);
			view.SetBackgroundResource(typedValue.ResourceId);
		}
		public static void SetWindowInsetsFlags(this View view, WindowInsetsFlags flags) {
			ViewUtils.DoOnApplyWindowInsets(view, new OnApplyWindowInsetsListener(flags));
		}

		private static Context Context => Platform.CurrentActivity ?? Application.Context;
	}
}