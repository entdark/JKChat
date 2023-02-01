using Android.OS;
using Android.Views;
using Android.Widget;

using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Main;
using JKChat.Core.ViewModels.ServerList;

using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using MvvmCross.Platforms.Android.Views.Fragments;

namespace JKChat.Android.Views.ServerList {
	[MvxFragmentPresentation(typeof(MainViewModel), Resource.Id.content_modal, true, Resource.Animation.modal_slide_btt, Resource.Animation.modal_slide_ttb, Resource.Animation.modal_slide_btt, Resource.Animation.modal_slide_ttb)]
//	[MvxDialogFragmentPresentation(true, typeof(MainViewModel), false, Resource.Animation.modal_slide_btt, Resource.Animation.modal_slide_ttb)]
	public class ServerListPickerFragment : BaseFragment<ServerListPickerViewModel> {
		public ServerListPickerFragment() : base(Resource.Layout.server_list_picker_page) {
		}

/*		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			this.EnsureBindingContextIsSet(inflater);
			return new FrameLayout(this.Context);
		}*/

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);
			if (BackArrow != null) {
				BackArrow.AlwaysClose = true;
				BackArrow.SetRotation(1.0f, false);
			}
			/*var recyclerView = new MvxRecyclerView(this.Context, null, Resource.Style.RecyclerView, new MvxRecyclerAdapter((IMvxAndroidBindingContext)BindingContext));//view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			recyclerView.ItemsSource = ViewModel.Items;
			recyclerView.ItemTemplateId = Resource.Layout.server_list_item;
			var viewGroup = view as ViewGroup;
			viewGroup.AddView(recyclerView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));*/
		}

		protected override void ActivityExit() {}

		protected override void ActivityPopEnter() {}
	}
}