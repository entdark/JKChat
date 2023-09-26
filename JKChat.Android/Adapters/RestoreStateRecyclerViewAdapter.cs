using System.Collections.Specialized;

using Android.OS;

using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;

namespace JKChat.Android.Adapters {
	public class RestoreStateRecyclerAdapter : BaseRecyclerViewAdapter {
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
}