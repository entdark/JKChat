using System;

using Android.Content;
using Android.Views;

using AndroidX.Core.View;

using Google.Android.Material.Internal;

using JKChat.Android.Presenter;
using JKChat.Android.Services;
using JKChat.Android.Views.Base;
using JKChat.Core;
using JKChat.Core.Navigation;
using JKChat.Core.Services;
using JKChat.Core.ValueCombiners;

using Microsoft.Extensions.Logging;

using MvvmCross;
using MvvmCross.Binding;
using MvvmCross.Binding.Combiners;
using MvvmCross.IoC;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Android;
using MvvmCross.Platforms.Android.Binding;
using MvvmCross.Platforms.Android.Binding.Binders;
using MvvmCross.Platforms.Android.Core;
using MvvmCross.Platforms.Android.Presenters;
using MvvmCross.Plugin;
using MvvmCross.ViewModels;
using MvvmCross.Views;

using Serilog;
using Serilog.Extensions.Logging;

namespace JKChat.Android {
	public class Setup : MvxAndroidSetup<App> {
		protected override IMvxNavigationService CreateNavigationService(IMvxIoCProvider iocProvider) {
			iocProvider.LazyConstructAndRegisterSingleton<IMvxNavigationService, IMvxViewModelLoader, IMvxViewDispatcher, IMvxIoCProvider>(
				(loader, dispatcher, iocProvider) => new NavigationService(loader, dispatcher, iocProvider));
			var navigationService = iocProvider.Resolve<IMvxNavigationService>();
			iocProvider.RegisterSingleton(navigationService as INavigationService);
			return navigationService;
		}

		protected override IMvxAndroidViewPresenter CreateViewPresenter() {
			return new AndroidViewPresenter(AndroidViewAssemblies);
		}

		protected override MvxBindingBuilder CreateBindingBuilder() {
			return new InsetsAndroidBindingBuilder();
		}

		protected override void InitializeFirstChance(IMvxIoCProvider iocProvider) {
			iocProvider.RegisterSingleton<IDialogService>(() => new DialogService());
			iocProvider.RegisterSingleton<IAppService>(() => new AppService());
			iocProvider.RegisterSingleton<INotificationsService>(() => new NotificationsService());
			base.InitializeFirstChance(iocProvider);
		}

		protected override void FillValueCombiners(IMvxValueCombinerRegistry registry) {
			base.FillValueCombiners(registry);
			registry.AddOrOverwrite("ColourTextParameter", new ColourTextParameterValueCombiner());
		}

		public override void LoadPlugins(IMvxPluginManager pluginManager) {
			base.LoadPlugins(pluginManager);
			pluginManager.EnsurePluginLoaded<MvvmCross.Plugin.Visibility.Platforms.Android.Plugin>(true);
		}

		protected override ILoggerProvider CreateLogProvider() {
			return new SerilogLoggerProvider();
		}

		protected override ILoggerFactory CreateLogFactory() {
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				// add more sinks here
				.WriteTo.AndroidLog()
				.CreateLogger();

			return new SerilogLoggerFactory();
		}

		private class InsetsAndroidBindingBuilder : MvxAndroidBindingBuilder {
			protected override IMvxLayoutInflaterHolderFactoryFactory CreateLayoutInflaterFactoryFactory() {
				return new InsetsLayoutInflaterFactoryFactory();
			}

			private class InsetsLayoutInflaterFactoryFactory : IMvxLayoutInflaterHolderFactoryFactory {
				public IMvxLayoutInflaterHolderFactory Create(object source) {
					return new InsetsBindingLayoutInflaterFactory(source);
				}

				private class InsetsBindingLayoutInflaterFactory : MvxBindingLayoutInflaterFactory {
					public InsetsBindingLayoutInflaterFactory(object source) : base(source) {
					}

					public override View BindCreatedView(View view, Context context, global::Android.Util.IAttributeSet attrs) {
						using var typedArray = context.ObtainStyledAttributes(attrs, Resource.Styleable.View);
						int numStyles = typedArray.IndexCount;
						WindowInsetsFlags flags = WindowInsetsFlags.None;
						for (var i = 0; i < numStyles; ++i) {
							var attributeId = typedArray.GetIndex(i);

							bool attributeValue = typedArray.GetBoolean(attributeId, false);
							if (attributeValue) {
								flags |= attributeId switch {
									Resource.Styleable.View_paddingLeftFitsWindowInsets => WindowInsetsFlags.PaddingLeft,
									Resource.Styleable.View_paddingRightFitsWindowInsets => WindowInsetsFlags.PaddingRight,
									Resource.Styleable.View_paddingTopFitsWindowInsets => WindowInsetsFlags.PaddingTop,
									Resource.Styleable.View_paddingBottomFitsWindowInsets => WindowInsetsFlags.PaddingBottom,

									Resource.Styleable.View_paddingLeftFitsWindowInsetsButExpanded => WindowInsetsFlags.PaddingLeftButExpanded,
									Resource.Styleable.View_paddingRightFitsWindowInsetsButExpanded => WindowInsetsFlags.PaddingRightButExpanded,
									Resource.Styleable.View_paddingTopFitsWindowInsetsButExpanded => WindowInsetsFlags.PaddingTopButExpanded,
									Resource.Styleable.View_paddingBottomFitsWindowInsetsButExpanded => WindowInsetsFlags.PaddingBottomButExpanded,

									Resource.Styleable.View_marginLeftFitsWindowInsets => WindowInsetsFlags.MarginLeft,
									Resource.Styleable.View_marginRightFitsWindowInsets => WindowInsetsFlags.MarginRight,
									Resource.Styleable.View_marginTopFitsWindowInsets => WindowInsetsFlags.MarginTop,
									Resource.Styleable.View_marginBottomFitsWindowInsets => WindowInsetsFlags.MarginBottom,

									_ => WindowInsetsFlags.None
								};
							}
						}
						if (flags != WindowInsetsFlags.None)
							ViewUtils.DoOnApplyWindowInsets(view, new OnApplyWindowInsetsListener(view, flags));
						typedArray.Recycle();
						return base.BindCreatedView(view, context, attrs);
					}
				}
			}

			private class OnApplyWindowInsetsListener : Java.Lang.Object, ViewUtils.IOnApplyWindowInsetsListener {
				private readonly WindowInsetsFlags flags;

				private ViewGroup.MarginLayoutParams initialLayoutParameters;

				public OnApplyWindowInsetsListener(View view, WindowInsetsFlags flags) {
//					this.initialLayoutParameters = view.LayoutParameters is ViewGroup.MarginLayoutParams marginLayoutParameters ? new ViewGroup.MarginLayoutParams(marginLayoutParameters) : null;
					this.flags = flags;
				}

				public WindowInsetsCompat OnApplyWindowInsets(View view, WindowInsetsCompat insets, ViewUtils.RelativePadding initialPadding) {
					bool isExpanded = (Mvx.IoCProvider.Resolve<IMvxAndroidCurrentTopActivity>().Activity as IBaseActivity)?.ExpandedWindow ?? false;
					bool paddingTop = flags.HasFlag(WindowInsetsFlags.PaddingTop) || (!isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingTopButExpanded));
					bool paddingBottom = flags.HasFlag(WindowInsetsFlags.PaddingBottom) || (!isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingBottomButExpanded));
					bool paddingLeft = flags.HasFlag(WindowInsetsFlags.PaddingLeft) || (!isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingLeftButExpanded));
					bool paddingRight = flags.HasFlag(WindowInsetsFlags.PaddingRight) || (!isExpanded && flags.HasFlag(WindowInsetsFlags.PaddingRightButExpanded));

					bool isRtl = ViewCompat.GetLayoutDirection(view) == ViewCompat.LayoutDirectionRtl;
					int insetTop = insets.GetInsets(WindowInsetsCompat.Type.SystemBars()).Top;
					int insetBottom = insets.GetInsets(WindowInsetsCompat.Type.SystemBars()).Bottom;
					int insetLeft = insets.GetInsets(WindowInsetsCompat.Type.SystemBars()).Left;
					int insetRight = insets.GetInsets(WindowInsetsCompat.Type.SystemBars()).Right;

					initialPadding.Top += paddingTop ? insetTop : 0;
					initialPadding.Bottom += paddingBottom ? insetBottom : 0;
					int systemWindowInsetLeft = paddingLeft ? insetLeft : 0;
					int systemWindowInsetRight = paddingRight ? insetRight : 0;
					initialPadding.Start += isRtl ? systemWindowInsetRight : systemWindowInsetLeft;
					initialPadding.End += isRtl ? systemWindowInsetLeft : systemWindowInsetRight;
					initialPadding.ApplyToView(view);

					if (view.LayoutParameters is ViewGroup.MarginLayoutParams marginLayoutParameters && initialLayoutParameters == null) {
						initialLayoutParameters = new ViewGroup.MarginLayoutParams(marginLayoutParameters);
					}

					if (initialLayoutParameters != null) {
						bool marginTop = flags.HasFlag(WindowInsetsFlags.MarginTop);
						bool marginBottom = flags.HasFlag(WindowInsetsFlags.MarginBottom);
						bool marginLeft = flags.HasFlag(WindowInsetsFlags.MarginLeft);
						bool marginRight = flags.HasFlag(WindowInsetsFlags.MarginRight);

						if (marginTop || marginBottom || marginLeft || marginRight) {
							var newLayoutParameters = view.LayoutParameters as ViewGroup.MarginLayoutParams;
							newLayoutParameters.TopMargin = initialLayoutParameters.TopMargin + (marginTop ? insetTop : 0);
							newLayoutParameters.BottomMargin = initialLayoutParameters.BottomMargin + (marginBottom ? insetBottom : 0);
							newLayoutParameters.LeftMargin = initialLayoutParameters.LeftMargin + (marginLeft ? insetLeft : 0);
							newLayoutParameters.RightMargin = initialLayoutParameters.RightMargin + (marginRight ? insetRight : 0);
							view.LayoutParameters = newLayoutParameters;
						}
					}

					return insets;
				}
			}

			[Flags]
			private enum WindowInsetsFlags {
				None						= 0,

				PaddingLeft					= 1 << 0,
				PaddingRight				= 1 << 1,
				PaddingTop					= 1 << 2,
				PaddingBottom				= 1 << 3,

				PaddingLeftButExpanded		= 1 << 4,
				PaddingRightButExpanded		= 1 << 5,
				PaddingTopButExpanded		= 1 << 6,
				PaddingBottomButExpanded	= 1 << 7,

				MarginLeft					= 1 << 8,
				MarginRight					= 1 << 9,
				MarginTop					= 1 << 10,
				MarginBottom				= 1 << 11
			}
		}
	}
}