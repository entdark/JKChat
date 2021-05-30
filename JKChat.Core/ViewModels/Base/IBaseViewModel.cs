using System;
using System.Collections.Generic;
using System.Text;

namespace JKChat.Core.ViewModels.Base {
	public interface IBaseViewModel {
		string Title { get; set; }
		bool IsLoading { get; set; }
	}
}
