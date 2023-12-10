namespace JKChat.Core.Models {
	public enum WidgetLink {
		ServerInfo,
		Chat,
		Application
	}
	public static class WidgetLinkExtensions {
		public static string ToDisplayString(this WidgetLink widgetLink) {
			return widgetLink switch {
				WidgetLink.Chat => "Chat",
				WidgetLink.Application => "Application",
				_ => "Server Info"
			};
		}
	}
}