using System;
using System.Collections.Specialized;

using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.FloatingActionButton;
using JKChat.Android.Helpers;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Main;
using JKChat.Core.ViewModels.ServerList;
using JKChat.Core.ViewModels.ServerList.Items;

using MvvmCross.Commands;
using MvvmCross.DroidX;
using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;

namespace JKChat.Android.Views.ServerList {
	[BottomNavigationViewPresentation(
		"Server List",
		Resource.Id.content_viewpager,
		Resource.Id.navigationview,
		Resource.Drawable.ic_server_list,
		typeof(MainViewModel)
	)]
	//[MvxFragmentPresentation(typeof(MainViewModel), Resource.Id.content_frame, false)]
	public class ServerListFragment : ReportFragment<ServerListViewModel, ServerListItemVM> {
		//private IMenuItem copyItem;
		private IMenuItem addItem;
		private MvxRecyclerView recyclerView;
		private FloatingActionButton addButton;

        public ServerListFragment() : base(Resource.Layout.server_list_page, Resource.Menu.server_list_toolbar_item) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			BackArrow.AlwaysClose = true;

			recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			recyclerView.Adapter = new RestoreStateRecyclerAdapter((IMvxAndroidBindingContext)BindingContext, recyclerView);

			var refreshLayout = view.FindViewById<MvxSwipeRefreshLayout>(Resource.Id.mvxswiperefreshlayout);
			refreshLayout.SetColorSchemeResources(Resource.Color.accent);
			refreshLayout.SetProgressBackgroundColorSchemeResource(Resource.Color.primary);
			addButton = view.FindViewById<FloatingActionButton>(Resource.Id.add_button);
			recyclerView.AddOnScrollListener(new OnScrollListener((dx, dy) => {
				if (SelectedItem != null) {
					return;
				}
				if (dy > 0) {
					addButton.Hide();
				} else if (dy < 0) {
					addButton.Show();
				}
			}));
        }

		public override void OnDestroyView() {
			base.OnDestroyView();
		}

		public override void OnResume() {
			base.OnResume();
		}

		public override void OnPause() {
			base.OnPause();
		}

		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater) {
			inflater.Inflate(MenuId, menu);
			//copyItem = menu.FindItem(Resource.Id.copy_item);
			base.OnCreateOptionsMenu(menu, inflater);
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			if (item == addItem) {
				ViewModel.AddServerCommand?.Execute(SelectedItem);
				return true;
			}
			return base.OnOptionsItemSelected(item);
		}

		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();

            addItem = Menu.FindItem(Resource.Id.add_item);
            addItem.SetClickAction(() => {
				this.OnOptionsItemSelected(addItem);
			});
			addItem?.SetVisible(false, false);
//            CheckSelection();
        }

		protected override void ActivityExit() {}

		protected override void ActivityPopEnter() {}

		protected override void CheckSelection(bool animated = true) {
			if (SelectedItem != null) {
				addButton?.Hide();
			} else {
				addButton?.Show();
			}
			base.CheckSelection(animated);
		}

		protected override void ShowSelection(bool animated = true) {
			SetUpNavigation(true);
//			addItem?.SetVisible(false, false);
			base.ShowSelection(animated);
		}

		protected override void CloseSelection(bool animated = true) {
			BackArrow?.SetRotation(0.0f, false);
			SetUpNavigation(false);
//            addItem?.SetVisible(true, animated);
            base.CloseSelection(animated);
		}

		public class RestoreStateRecyclerAdapter : MvxRecyclerAdapter {
			private readonly MvxRecyclerView recyclerView;
			private IParcelable recyclerViewSavedState;

			public RestoreStateRecyclerAdapter(IMvxAndroidBindingContext bindingContext, MvxRecyclerView recyclerView) : base(bindingContext) {
				this.recyclerView = recyclerView;
			}

			public override void NotifyDataSetChanged(NotifyCollectionChangedEventArgs ev) {
				bool moved = ev.Action == NotifyCollectionChangedAction.Move;
				if (moved) {
					recyclerViewSavedState = recyclerView?.GetLayoutManager()?.OnSaveInstanceState();
				}
				base.NotifyDataSetChanged(ev);
				if (moved) {
					recyclerView?.GetLayoutManager()?.OnRestoreInstanceState(recyclerViewSavedState);
				}
				if (ev.Action == NotifyCollectionChangedAction.Add) {
					recyclerView?.ScrollToPosition(0);
				}
			}
		}

		private class OnScrollListener : RecyclerView.OnScrollListener {
			private readonly Action<int, int> onScrolled;
			public OnScrollListener(Action<int, int> onScrolled) {
				this.onScrolled = onScrolled;
			}
            public override void OnScrolled(RecyclerView recyclerView, int dx, int dy) {
                base.OnScrolled(recyclerView, dx, dy);
				onScrolled?.Invoke(dx, dy);
            }
        }
	}
}