using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using Android.Animation;
using Android.OS;
using Android.Text;
using Android.Text.Method;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

using AndroidX.Lifecycle;
using AndroidX.RecyclerView.Widget;

using JKChat.Android.Controls;
using JKChat.Android.Helpers;
using JKChat.Android.Presenter.Attributes;
using JKChat.Android.Views.Base;
using JKChat.Android.Views.Main;
using JKChat.Core.Messages;
using JKChat.Core.ViewModels.Chat;
using JKChat.Core.ViewModels.Chat.Items;

using MvvmCross;
using MvvmCross.Binding.Extensions;
using MvvmCross.Commands;
using MvvmCross.DroidX.RecyclerView;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Plugin.Messenger;

using static JKChat.Android.ValueConverters.ColourTextValueConverter;

namespace JKChat.Android.Views.Chat {
	[PushFragmentPresentation]
	public class ChatFragment : ReportFragment<ChatViewModel, ChatItemVM> {
		private IMenuItem copyItem;
		private ImageView sendButton;
		private ScrollToBottomRecyclerAdapter scrollToBottomRecyclerAdapter;

		private string message;
		public string Message {
			get => message;
			set {
				if (string.IsNullOrEmpty(message) != string.IsNullOrEmpty(value)) {
					ScaleSendButton(!string.IsNullOrEmpty(value));
				}
				message = value;
			}
		}

		public ChatFragment() : base(Resource.Layout.chat_page, Resource.Menu.chat_toolbar_items) {}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			var view = base.OnCreateView(inflater, container, savedInstanceState);
			var set = this.CreateBindingSet();
			set.Bind(this).For(v => v.Message).To(vm => vm.Message);
			set.Apply();
			return view;
		}

		public override void OnViewCreated(View view, Bundle savedInstanceState) {
			base.OnViewCreated(view, savedInstanceState);

			var recyclerView = view.FindViewById<MvxRecyclerView>(Resource.Id.mvxrecyclerview);
			var layoutManager = new LinearLayoutManager(Context, LinearLayoutManager.Vertical, false) {
				StackFromEnd = true
			};
			recyclerView.SetLayoutManager(layoutManager);
			if (scrollToBottomRecyclerAdapter == null) {
				scrollToBottomRecyclerAdapter = new ScrollToBottomRecyclerAdapter((IMvxAndroidBindingContext)BindingContext, recyclerView) {
					AdjustItem = (vh, pos) => {
						var textView = vh.ItemView.FindViewById<LinkTextView>(Resource.Id.message);
						textView.MovementMethod = LongClickLinkMovementMethod.Instance;
					}
				};
			}
			scrollToBottomRecyclerAdapter.Start();
			recyclerView.Adapter = scrollToBottomRecyclerAdapter;

			sendButton = view.FindViewById<ImageView>(Resource.Id.send_button);
			ScaleSendButton(!string.IsNullOrEmpty(ViewModel.Message), true);

			var titleView = this.BindingInflate(Resource.Layout.chat_title, null, false);
			SetCustomView(titleView);
		}

		public override void OnDestroyView() {
			if (scrollToBottomRecyclerAdapter != null) {
				scrollToBottomRecyclerAdapter.Finish();
				//scrollToBottomRecyclerAdapter = null;
			}
			//CloseSelection();
			//base.DisplayCustomTitle(false);
			base.OnDestroyView();
		}

		public override void OnDestroy() {
			base.OnDestroy();
		}

		public override void OnPause() {
			base.OnPause();
		}

		public override void OnResume() {
			base.OnResume();
			scrollToBottomRecyclerAdapter?.ScrollToBottom();
		}

		public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater) {
			base.OnCreateOptionsMenu(menu, inflater);
		}

		protected override void CreateOptionsMenu() {
			base.CreateOptionsMenu();

			copyItem = Menu.FindItem(Resource.Id.copy_item);
			copyItem.SetClickAction(() => {
				this.OnOptionsItemSelected(copyItem);
			});
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			if (SelectedItem != null) {
				if (item == copyItem) {
					ViewModel.CopyCommand?.Execute(SelectedItem);
				}
				return base.OnOptionsItemSelected(item);
			}
			if (ViewModel == null) {
				return base.OnOptionsItemSelected(item);
			}
			Task.Run(ViewModel.OfferDisconnect);
			return true;
		}

		protected override void ShowSelection(bool animated = true) {
			base.ShowSelection(animated);
			copyItem?.SetVisible(true, animated);
			DisplayCustomTitle(false);
		}

		protected override void CloseSelection(bool animated = true) {
			base.CloseSelection(animated);
			copyItem?.SetVisible(false, animated);
			DisplayCustomTitle(true);
		}

		protected override void DisplayCustomTitle(bool show) {
			base.DisplayCustomTitle(SelectedItem == null);
		}

		protected override void BackNavigationClick(object sender, AndroidX.AppCompat.Widget.Toolbar.NavigationClickEventArgs ev) {
			if (!OnBackPressed()) {
				Task.Run(ViewModel.OfferDisconnect);
			}
		}

		private void ScaleSendButton(bool show, bool instant = false) {
			float scale = show ? 1.0f : 0.0f;
			if (instant) {
				sendButton.ScaleX = scale;
				sendButton.ScaleY = scale;
				return;
			}
			var scaleX = ObjectAnimator.OfFloat(sendButton, "scaleX", scale);
			var scaleY = ObjectAnimator.OfFloat(sendButton, "scaleY", scale);
			var set = new AnimatorSet();
			set.PlayTogether(scaleX, scaleY);
			set.SetDuration(200);
			set.SetInterpolator(new DecelerateInterpolator());
			set.Start();
		}

		private class ScrollToBottomRecyclerAdapter : MvxRecyclerAdapter {
			private readonly MvxRecyclerView recyclerView;
			private bool dragging = false, touched = false;
			private MvxSubscriptionToken sentMessageMessageToken;
			private ScrollToBottomOnScrollListener onScrollListener;

			public bool ScrolledToBottom { get; set; } = true;
			public Action<RecyclerView.ViewHolder, int> AdjustItem { get; set; }

			public ScrollToBottomRecyclerAdapter(IMvxAndroidBindingContext bindingContext, MvxRecyclerView recyclerView) : base(bindingContext) {
				this.recyclerView = recyclerView;
			}

			private void RecyclerViewTouch(object sender, View.TouchEventArgs ev) {
				if (ev.Event.Action == MotionEventActions.Down) {
					dragging = true;
				} else if (ev.Event.Action == MotionEventActions.Up) {
					dragging = false;
				}
				touched = true;
				ev.Handled = false;
			}

			private void OnSentMessageMessage(SentMessageMessage message) {
				int position = recyclerView.ItemsSource.Count()-1;
				if (position >= 0) {
					recyclerView.ScrollToPosition(position);
				}
			}

			public override void NotifyDataSetChanged(NotifyCollectionChangedEventArgs ev) {
				base.NotifyDataSetChanged(ev);

				bool activityResumed = Xamarin.Essentials.Platform.CurrentActivity is MainActivity mainActivity
					&& mainActivity.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Resumed);

				if (activityResumed && ScrolledToBottom && ev.Action == NotifyCollectionChangedAction.Add && ev.NewStartingIndex >= 0 && !dragging) {
					ScrollToBottom();
				}
			}

			public void ScrollToBottom() {
				int position = recyclerView.ItemsSource.Count()-1;
				if (ScrolledToBottom && position >= 0) {
					recyclerView.ScrollToPosition(position);
					return;
					int visiblePosition;
					if (recyclerView.GetLayoutManager() is LinearLayoutManager linearLayoutManager) {
						visiblePosition = linearLayoutManager.FindLastVisibleItemPosition();
					} else {
						visiblePosition = -1;
					}
					if (visiblePosition == -1 || (position - visiblePosition) > 5) {
						recyclerView.ScrollToPosition(position);
					} else {
						recyclerView.SmoothScrollToPosition(position);
					}
				}
			}

			public void Start() {
				onScrollListener = new ScrollToBottomOnScrollListener((idle) => {
					if (idle && touched) {
						this.ScrolledToBottom = idle && !recyclerView.CanScrollVertically(2);
						touched = false;
					}
				});
				this.recyclerView.AddOnScrollListener(onScrollListener);
				this.recyclerView.Touch += RecyclerViewTouch;
				sentMessageMessageToken = Mvx.IoCProvider.Resolve<IMvxMessenger>().Subscribe<SentMessageMessage>(OnSentMessageMessage);
			}

			public void Finish() {
				if (this.recyclerView != null) {
					this.recyclerView.Touch -= RecyclerViewTouch;
				}
				if (sentMessageMessageToken != null) {
					Mvx.IoCProvider.Resolve<IMvxMessenger>().Unsubscribe<SentMessageMessage>(sentMessageMessageToken);
					sentMessageMessageToken = null;
				}
				if (onScrollListener != null) {
					recyclerView?.RemoveOnScrollListener(onScrollListener);
					onScrollListener = null;
				}
			}

			protected override void Dispose(bool disposing) {
				if (disposing) {
					Finish();
				}
				base.Dispose(disposing);
			}

			public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position) {
				base.OnBindViewHolder(holder, position);
				AdjustItem?.Invoke(holder, position);
			}

			private class ScrollToBottomOnScrollListener : RecyclerView.OnScrollListener {
				private Action<bool> scrollStateChangedCallback;
				public ScrollToBottomOnScrollListener(Action<bool> scrollStateChangedCallback) {
					this.scrollStateChangedCallback = scrollStateChangedCallback;
				}
				public override void OnScrollStateChanged(RecyclerView recyclerView, int newState) {
					bool idle = newState == RecyclerView.ScrollStateIdle;
					scrollStateChangedCallback?.Invoke(idle);
					base.OnScrollStateChanged(recyclerView, newState);
				}
			}
		}

		public class LongClickLinkMovementMethod : LinkMovementMethod {
			private static readonly int LongClickTime = ViewConfiguration.LongPressTimeout;
			private Handler longClickHandler;
			private bool isLongPressed = false;

			public override bool OnTouchEvent(TextView widget, ISpannable buffer, MotionEvent ev) {
				var action = ev.Action;

				if (action == MotionEventActions.Cancel) {
					longClickHandler?.RemoveCallbacksAndMessages(null);
				}

				if (action == MotionEventActions.Up || action == MotionEventActions.Down) {
					int x = (int)ev.GetX();
					int y = (int)ev.GetY();

					x -= widget.TotalPaddingLeft;
					y -= widget.TotalPaddingTop;

					x += widget.ScrollX;
					y += widget.ScrollY;

					Layout layout = widget.Layout;
					int line = layout.GetLineForVertical(y);
					int offset = layout.GetOffsetForHorizontal(line, x);

					var link = buffer.GetSpans(offset, offset, Java.Lang.Class.FromType(typeof(LinkClickableSpan)));

					if (action == MotionEventActions.Up) {
						longClickHandler?.RemoveCallbacksAndMessages(null);
						if (link.Length != 0 && !isLongPressed) {
							(link[0] as LinkClickableSpan).OnClick(widget);
						}
						isLongPressed = false;
						//(widget.Parent.Parent as View).OnTouchEvent(ev);
					} else if (action == MotionEventActions.Down) {
						if (link.Length != 0) {
							Selection.SetSelection(buffer, buffer.GetSpanStart(link[0]), buffer.GetSpanEnd(link[0]));
						} else {
							//(widget.Parent.Parent as View);
						}

						longClickHandler.PostDelayed(() => {
							isLongPressed = true;
							(widget.Parent.Parent as View).PerformLongClick();
						}, LongClickTime);
					}
					//base.OnTouchEvent(widget, buffer, ev);
					return true;
				}

				return base.OnTouchEvent(widget, buffer, ev);
			}


			public new static IMovementMethod Instance {
				get {
					if (instance == null) {
						instance = new LongClickLinkMovementMethod();
						instance.longClickHandler = new Handler();
					}
					return instance;
				}
			}
			private static LongClickLinkMovementMethod instance;
		}
	}
}