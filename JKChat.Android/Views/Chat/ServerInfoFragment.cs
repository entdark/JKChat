using System;

using Android.OS;
using Android.Views;

using AndroidX.AppCompat.View.Menu;
using AndroidX.Core.Content;
using AndroidX.ViewPager2.Widget;

using Google.Android.Material.Button;
using Google.Android.Material.Tabs;

using Java.Util.Concurrent;

using JKChat.Android.Helpers;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Chat;

using MvvmCross.Binding.BindingContext;
using MvvmCross.DroidX.RecyclerView;
using MvvmCross.DroidX.RecyclerView.ItemTemplates;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Views;

using static JKChat.Core.ViewModels.Chat.ServerInfoViewModel;

namespace JKChat.Android.Views.Chat {
	[PushFragmentPresentation]
	public class ServerInfoFragment : BaseFragment<ServerInfoViewModel> {
		private IMenuItem favouriteItem;
		private MaterialButton connectButton;

		private bool needPassword;
		public bool NeedPassword {
			get => needPassword;
			set {
				needPassword = value;
				UpdateConnectButton();
			}
		}

		private bool isFavourite;
		public bool IsFavourite {
			get => isFavourite;
			set {
				isFavourite = value;
				UpdateFavouriteItem();
			}
		}

		public ServerInfoFragment() : base(Resource.Layout.server_info_page, Resource.Menu.server_info_toolbar_items) {
			PostponeTransition = true;
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			var view = base.OnCreateView(inflater, container, savedInstanceState);
			using var set = this.CreateBindingSet();
			set.Bind(this).For(v => v.IsFavourite).To(vm => vm.IsFavourite);
			return view;
		}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			connectButton = view.FindViewById<MaterialButton>(Resource.Id.connect_button);

			var tabLayout = view.FindViewById<TabLayout>(Resource.Id.tablayout);
			var viewPager = view.FindViewById<ViewPager2>(Resource.Id.viewpager);
			if (viewPager.Adapter is not MvxRecyclerAdapter adapter)
				viewPager.Adapter = adapter = new MvxRecyclerAdapter((IMvxAndroidBindingContext)this.BindingContext) {
					ItemTemplateSelector = new ServerInfoTemplateSelector()
				};
			viewPager.OffscreenPageLimit = 2;
			var mediator = new TabLayoutMediator(tabLayout, viewPager, new TabConfigurationStrategy((tab, position) => {
				string tabText = ViewModel.AllSecondaryItems[position].TabTitle;
				tab.SetText(tabText);
			}));
			mediator.Attach();

			using var set = this.CreateBindingSet();
			set.Bind(adapter).For(a => a.ItemsSource).To(vm => vm.AllSecondaryItems);
			set.Bind(this).For(v => v.NeedPassword).To(vm => vm.NeedPassword);
		}

		protected override void BindTitle(MvxFluentBindingDescriptionSet<IMvxFragmentView<ServerInfoViewModel>, ServerInfoViewModel> set) {}

		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();
			if (Menu is MenuBuilder menuBuilder) {
				menuBuilder.SetOptionalIconsVisible(true);
			}
			favouriteItem = Menu.FindItem(Resource.Id.favourite_item);
			favouriteItem.SetClickAction(() => {
				ViewModel?.FavouriteCommand?.Execute();
			});
			UpdateFavouriteItem();
			var shareItem = Menu.FindItem(Resource.Id.share_item);
			shareItem.SetClickAction(() => {
				ViewModel?.ShareCommand?.Execute();
			});
			var reportServerItem = Menu.FindItem(Resource.Id.report_server_item);
			reportServerItem.AdjustIconInsets();
			reportServerItem.SetClickAction(() => {
				ViewModel?.ServerReportCommand?.Execute();
			});
		}

		private void UpdateFavouriteItem() {
			if (favouriteItem == null)
				return;
			favouriteItem.SetIcon(IsFavourite ? Resource.Drawable.ic_star_filled : Resource.Drawable.ic_star_outlined);
		}

		private void UpdateConnectButton() {
			connectButton?.ToggleIconButton(Resource.Drawable.ic_lock, NeedPassword);
		}

		private class TabConfigurationStrategy : Java.Lang.Object, TabLayoutMediator.ITabConfigurationStrategy {
			private readonly Action<TabLayout.Tab, int> configureTabAction;

			public TabConfigurationStrategy(Action<TabLayout.Tab, int> configureTabAction) {
				this.configureTabAction = configureTabAction;
			}

			public void OnConfigureTab(TabLayout.Tab tab, int position) {
				configureTabAction?.Invoke(tab, position);
			}
		}

		private class ServerInfoTemplateSelector : MvxTemplateSelector<TabItems> {
			private const int PlayersViewType = 0;
			private const int FullInfoViewType = 1;

			public override int GetItemLayoutId(int fromViewType) {
				return fromViewType switch {
					PlayersViewType => Resource.Layout.server_info_players,
					FullInfoViewType => Resource.Layout.server_info_full,
					_ => throw new Exception("View type is invalid")
				};
			}

			protected override int SelectItemViewType(TabItems forItemObject) {
				return forItemObject.TabIndex switch {
					0 => PlayersViewType,
					1 => FullInfoViewType,
					_ => throw new Exception("Item for view type is invalid")
				};
			}
		}
	}
}