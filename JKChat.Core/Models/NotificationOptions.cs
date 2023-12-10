using System;

namespace JKChat.Core.Models {
	[Flags]
	public enum NotificationOptions {
		None				= 0,
		Enabled				= 1 << 0,
		PrivateMessages		= 1 << 1,
		PlayerConnects		= 1 << 2,

		Default				= Enabled | PrivateMessages
	}
}