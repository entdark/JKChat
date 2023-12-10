using System;

using JKChat.Core.ViewModels.Chat.Items;

using MvvmCross.DroidX.RecyclerView.ItemTemplates;

namespace JKChat.Android.TemplateSelectors {
	public class ChatItemTemplateSelector : MvxTemplateSelector<ChatItemVM> {
		private const int MessageViewType = 0;
		private const int InfoViewType = 1;

		public override int GetItemLayoutId(int fromViewType) {
			return fromViewType switch {
				MessageViewType => Resource.Layout.chat_message_item,
				InfoViewType => Resource.Layout.chat_info_item,
				_ => throw new Exception("View type is invalid"),
			};
		}

		protected override int SelectItemViewType(ChatItemVM forItemObject) {
			return forItemObject switch {
				ChatMessageItemVM => MessageViewType,
				ChatInfoItemVM => InfoViewType,
				_ => throw new Exception("Item for view type is invalid")
			};
		}
	}
}