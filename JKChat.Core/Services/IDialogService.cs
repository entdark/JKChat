using System;
using System.Threading.Tasks;

using JKChat.Core.ViewModels.Dialog;

namespace JKChat.Core.Services {
	public interface IDialogService {
		bool Showing { get; }
		Task ShowAsync(JKDialogConfig config, Action completion = null);
		void SaveState();
		void RestoreState();
		void Stop(bool force = false);
	}

	public class JKDialogConfig {
		public string Title { get; set; }
		public string Message { get; set; }
		public string LeftButton { get; set; }
		public Action<object> LeftClick { get; set; }
		public string RightButton { get; set; }
		public Action<object> RightClick { get; set; }
		public Action<object> AnyClick { get; set; }
		public Action<object> BackgroundClick { get; set; }
		public string Input { get; set; }
		public DialogListViewModel ListViewModel { get; set; }
		public JKDialogType Type { get; set; } = JKDialogType.Title;
		public bool ImmediateResult { get; set; } = true;
	}

	[Flags]
	public enum JKDialogType {
		Title = 1,
		Message = 2,
		Input = 4,
		List = 8
	}
}
