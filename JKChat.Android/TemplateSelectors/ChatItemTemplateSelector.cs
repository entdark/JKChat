using System;

using JKChat.Core.ViewModels.Chat.Items;

using MvvmCross.DroidX.RecyclerView.ItemTemplates;

namespace JKChat.Android.TemplateSelectors {
	public class ChatItemTemplateSelector : MvxTemplateSelector<ChatItemVM> {
		private const int MessageViewType = 0;
		private const int InfoViewType = 1;

		public override int GetItemLayoutId(int fromViewType) {
			switch (fromViewType) {
			case MessageViewType:
				return Resource.Layout.chat_message_item;
			case InfoViewType:
				return Resource.Layout.chat_info_item;
			default:
				throw new Exception("View type is invalid");
			}
		}

		protected override int SelectItemViewType(ChatItemVM forItemObject) {
			if (forItemObject is ChatMessageItemVM) {
				return MessageViewType;
			} else if (forItemObject is ChatInfoItemVM) {
				return InfoViewType;
			} else {
				throw new Exception("Item for view type is invalid");
			}
		}
	}
}