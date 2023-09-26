using AndroidX.RecyclerView.Widget;

using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;

namespace JKChat.Android.Adapters {
	public class BaseRecyclerViewAdapter : MvxRecyclerAdapter {
		protected MvxRecyclerView RecyclerView { get; private set; }

		public AdjustHolderPositionDelegate AdjustHolderOnBind { get; set; }
		public AdjustHolderDelegate AdjustHolderOnRecycle { get; set; }

		public BaseRecyclerViewAdapter(IMvxAndroidBindingContext bindingContext) : base(bindingContext) {}

		public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position) {
			base.OnBindViewHolder(holder, position);
			AdjustHolderOnBind?.Invoke(holder, position);
		}

		public override void OnViewRecycled(Java.Lang.Object holder) {
			AdjustHolderOnRecycle?.Invoke(holder as RecyclerView.ViewHolder);
			base.OnViewRecycled(holder);
		}

		public override void OnAttachedToRecyclerView(RecyclerView recyclerView) {
			base.OnAttachedToRecyclerView(recyclerView);
			RecyclerView = recyclerView as MvxRecyclerView;
		}

		public override void OnDetachedFromRecyclerView(RecyclerView recyclerView) {
			RecyclerView = null;
			base.OnDetachedFromRecyclerView(recyclerView);
		}
	}

	public delegate void AdjustHolderPositionDelegate(RecyclerView.ViewHolder viewHolder, int position);
	public delegate void AdjustHolderDelegate(RecyclerView.ViewHolder viewHolder);
}