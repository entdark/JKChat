using System.Collections.Specialized;

using Android.App;
using Android.OS;
using Android.Views;

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
		private MvxRecyclerView recyclerView;

		public ServerListFragment() : base(Resource.Layout.server_list_page, Resource.Menu.server_list_toolbar_item) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			BackArrow.AlwaysClose = true;

			recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			recyclerView.Adapter = new RestoreStateRecyclerAdapter((IMvxAndroidBindingContext)BindingContext, recyclerView);
			recyclerView.ItemLongClick = new MvxCommand<ServerListItemVM>((item) => {
				ToggleSelection(item);
			});

			var refreshLayout = view.FindViewById<MvxSwipeRefreshLayout>(Resource.Id.mvxswiperefreshlayout);
			refreshLayout.SetColorSchemeResources(Resource.Color.accent);
			refreshLayout.SetProgressBackgroundColorSchemeResource(Resource.Color.primary);
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

		public override bool OnBackPressed() {
			if (SelectedItem != null) {
				CloseSelection();
				return true;
			}
			return base.OnBackPressed();
		}

		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater) {
			inflater.Inflate(MenuId, menu);
			//copyItem = menu.FindItem(Resource.Id.copy_item);
			base.OnCreateOptionsMenu(menu, inflater);
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			if (SelectedItem != null) {
				/*if (item == copyItem) {
					ViewModel.CopyCommand?.Execute(SelectedItem);
				}*/
			}
			return base.OnOptionsItemSelected(item);
		}

		protected override void ActivityExit() {}

		protected override void ActivityPopEnter() {}

		protected override void ShowSelection(ServerListItemVM item, bool animated = true) {
			SetUpNavigation(true);
			base.ShowSelection(item, animated);
		}

		protected override void CloseSelection(bool animated = true) {
			BackArrow?.SetRotation(0.0f, false);
			SetUpNavigation(false);
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
			}
		}
	}
}