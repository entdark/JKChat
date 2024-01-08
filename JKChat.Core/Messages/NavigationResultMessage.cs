using JKChat.Core.ViewModels.Base.Result;

using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.Messages {
    public class NavigationResultMessage<TResult> : MvxMessage {
        public new IResultSettingViewModel<TResult> Sender => (IResultSettingViewModel<TResult>)base.Sender;
        public TResult Result { get; init; }

        public NavigationResultMessage(IResultSettingViewModel<TResult> sender, TResult result) : base(sender) {
            Result = result;
        }
    }
}