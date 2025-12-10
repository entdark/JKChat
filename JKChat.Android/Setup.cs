using System;

using Android.Content;
using Android.Views;
using Android.Widget;

using JKChat.Android.Controls.Listeners;
using JKChat.Android.Helpers;
using JKChat.Android.Presenter;
using JKChat.Android.Services;
using JKChat.Android.TargetBindings;
using JKChat.Core;
using JKChat.Core.Navigation;
using JKChat.Core.Services;
using JKChat.Core.ValueCombiners;

using Microsoft.Extensions.Logging;

using MvvmCross.Binding;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.Bindings.Target.Construction;
using MvvmCross.Binding.Combiners;
using MvvmCross.Converters;
using MvvmCross.IoC;
using MvvmCross.Navigation;
using MvvmCross.Platforms.Android.Binding;
using MvvmCross.Platforms.Android.Binding.Binders;
using MvvmCross.Platforms.Android.Binding.Binders.ViewTypeResolvers;
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
			return new InsetsAndroidBindingBuilder(FillValueConverters, FillValueCombiners, FillTargetFactories, FillBindingNames, FillViewTypes, FillAxmlViewTypeResolver, FillNamespaceListViewTypeResolver);
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

		protected override void FillTargetFactories(IMvxTargetBindingFactoryRegistry registry) {
			base.FillTargetFactories(registry);
			registry.RegisterCustomBindingFactory<TextSwitcher>("TextFormatted", view => new TextSwitcherTextFormattedTargetBinding(view));
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

		private class InsetsAndroidBindingBuilder(Action<IMvxValueConverterRegistry> fillValueConverters, Action<IMvxValueCombinerRegistry> fillValueCombiners, Action<IMvxTargetBindingFactoryRegistry> fillTargetFactories, Action<IMvxBindingNameRegistry> fillBindingNames, Action<IMvxTypeCache> fillViewTypes, Action<IMvxAxmlNameViewTypeResolver> fillAxmlViewTypeResolver, Action<IMvxNamespaceListViewTypeResolver> fillNamespaceListViewTypeResolver)
			: MvxAndroidBindingBuilder(fillValueConverters, fillValueCombiners, fillTargetFactories, fillBindingNames, fillViewTypes, fillAxmlViewTypeResolver, fillNamespaceListViewTypeResolver) {
			protected override IMvxLayoutInflaterHolderFactoryFactory CreateLayoutInflaterFactoryFactory() {
				return new InsetsLayoutInflaterFactoryFactory();
			}

			private class InsetsLayoutInflaterFactoryFactory : IMvxLayoutInflaterHolderFactoryFactory {
				public IMvxLayoutInflaterHolderFactory Create(object source) {
					return new InsetsBindingLayoutInflaterFactory(source);
				}

				private class InsetsBindingLayoutInflaterFactory(object source) : MvxBindingLayoutInflaterFactory(source) {
					public override View BindCreatedView(View view, Context context, global::Android.Util.IAttributeSet attrs) {
						using var typedArray = context.ObtainStyledAttributes(attrs, Resource.Styleable.View);
						int numStyles = typedArray.IndexCount;
						WindowInsetsFlags flags = WindowInsetsFlags.None;
						for (int i = 0; i < numStyles; ++i) {
							int attributeId = typedArray.GetIndex(i);

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

									Resource.Styleable.View_paddingLeftFitsWindowInsetsWhenExpanded => WindowInsetsFlags.PaddingLeftWhenExpanded,
									Resource.Styleable.View_paddingRightFitsWindowInsetsWhenExpanded => WindowInsetsFlags.PaddingRightWhenExpanded,
									Resource.Styleable.View_paddingTopFitsWindowInsetsWhenExpanded => WindowInsetsFlags.PaddingTopWhenExpanded,
									Resource.Styleable.View_paddingBottomFitsWindowInsetsWhenExpanded => WindowInsetsFlags.PaddingBottomWhenExpanded,

									Resource.Styleable.View_marginLeftFitsWindowInsets => WindowInsetsFlags.MarginLeft,
									Resource.Styleable.View_marginRightFitsWindowInsets => WindowInsetsFlags.MarginRight,
									Resource.Styleable.View_marginTopFitsWindowInsets => WindowInsetsFlags.MarginTop,
									Resource.Styleable.View_marginBottomFitsWindowInsets => WindowInsetsFlags.MarginBottom,

									_ => WindowInsetsFlags.None
								};
							}
						}
						if (flags != WindowInsetsFlags.None)
							view.SetWindowInsetsFlags(flags);
						typedArray.Recycle();
						return base.BindCreatedView(view, context, attrs);
					}
				}
			}
		}
	}
}