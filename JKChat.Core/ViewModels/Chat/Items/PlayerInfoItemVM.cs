using JKChat.Core.Models;
using JKChat.Core.ViewModels.Base.Items;

namespace JKChat.Core.ViewModels.Chat.Items {
	public class PlayerInfoItemVM : KeyValueItemVM {
		private Team team;
		public virtual Team Team {
			get => team;
			set => SetProperty(ref team, value);
		}
	}
}