using Android.OS;
using Android.Views;

using AndroidX.Core.Content;

using Google.Android.Material.Button;

using JKChat.Android.Adapters;
using JKChat.Android.Helpers;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Core.ViewModels.Favourites;
using JKChat.Core.ViewModels.ServerList.Items;

using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;

namespace JKChat.Android.Views.Favourites {
	[TabFragmentPresentation("Favourites", Resource.Drawable.ic_favourites_states)]
	public class FavouritesFragment : BaseFragment<FavouritesViewModel> {
		public FavouritesFragment() : base(Resource.Layout.favourites_page) {}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			var recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			if (recyclerView.Adapter is not RestoreStateRecyclerAdapter)
				recyclerView.Adapter = new RestoreStateRecyclerAdapter((IMvxAndroidBindingContext)BindingContext, recyclerView) {
					AdjustHolderOnBind = (viewHolder, position) => {
						if (viewHolder is IMvxRecyclerViewHolder { DataContext: ServerListItemVM item }) {
							var connectButton = viewHolder.ItemView.FindViewById<MaterialButton>(Resource.Id.connect_button);
							bool needIcon = item.NeedPassword;
							if (needIcon && connectButton.Icon == null) {
								connectButton.Icon = ContextCompat.GetDrawable(viewHolder.ItemView.Context, Resource.Drawable.ic_lock);
								connectButton.SetPadding(Context.GetDimensionInPx(Resource.Dimension.m3_btn_icon_btn_padding_left), connectButton.PaddingTop, Context.GetDimensionInPx(Resource.Dimension.m3_btn_icon_btn_padding_right), connectButton.PaddingBottom);
							} else if (!needIcon && connectButton.Icon != null) {
								connectButton.Icon = null;
								connectButton.SetPadding(Context.GetDimensionInPx(Resource.Dimension.m3_btn_padding_left), connectButton.PaddingTop, Context.GetDimensionInPx(Resource.Dimension.m3_btn_padding_right), connectButton.PaddingBottom);
							}
						}
					}
				};

			SetUpNavigation(false);
		}
	}
}