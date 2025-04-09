using System.Timers;

namespace JKChat.Core.ViewModels.Chat.Items {
	public class ChatInfoItemVM : ChatItemVM {
		private Timer timer;

		private string text;
		public string Text {
			get => text;
			internal set {
				text = value;
				if (timer == null) {
					timer = new Timer(256.0);
					timer.Elapsed += TimerElapsed;
					timer.Start();
				} else {
					timer.Interval = 256.0;
				}
			}
		}

		public bool Shadow { get; init; }
		public bool MergeNext { get; set; }
		public ChatInfoItemVM(string text, bool shadow = false, bool mergeNext = false) {
			this.text = text;
			Shadow = shadow;
			MergeNext = mergeNext;
		}

		private void TimerElapsed(object sender, ElapsedEventArgs ev) {
			RaisePropertyChanged(nameof(Text));
			timer.Elapsed -= TimerElapsed;
			timer = null;
		}
	}
}