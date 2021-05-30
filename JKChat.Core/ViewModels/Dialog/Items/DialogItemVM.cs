using System;
using System.Collections.Generic;
using System.Text;
using MvvmCross.ViewModels;

namespace JKChat.Core.ViewModels.Dialog.Items {
	public class DialogItemVM : MvxViewModel {
		public string Name { get; set; }

		public int Id { get; set; }

		private bool isSelected;
		public bool IsSelected {
			get => isSelected;
			set => SetProperty(ref isSelected, value);
		}
	}
}
