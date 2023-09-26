using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Base.Items {
	public class KeyValueItemVM : MvxNotifyPropertyChanged {
		internal object Data { get; set; }
		private string key;
		public virtual string Key {
			get => key;
			set => SetProperty(ref key, value);
		}
		private string val;
		public virtual string Value {
			get => val;
			set => SetProperty(ref val, value);
		}
	}
}