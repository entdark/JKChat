using System;

namespace JKChat.Core.Models {
	[Flags]
	public enum MinimapOptions {
		None			= 0,
		Enabled			= 1 << 0,
		Players			= 1 << 1,
		Names			= 1 << 2,
		Weapons			= 1 << 3,
		Flags			= 1 << 4,
		Predicted		= 1 << 5,
		FirstUnfocus	= 1 << 6,
		RememberFocus	= 1 << 7,
		AutoDownload	= 1 << 8,
		HighPerformance	= 1 << 9,

		Default			= Enabled | Players | Names | Weapons | Flags | Predicted | FirstUnfocus | RememberFocus | AutoDownload
	}
}