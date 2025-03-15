namespace JKChat.Core.Models;

public enum WidgetLink {
	ServerInfo,
	Chat,
	Application,
	ChatIfConnected
}

public static class WidgetLinkExtensions {
	public static string ToDisplayString(this WidgetLink widgetLink) {
		return widgetLink switch {
			WidgetLink.Chat => "Chat",
			WidgetLink.Application => "Application",
			WidgetLink.ChatIfConnected => "Chat if connected",
			_ => "Server Info"
		};
	}
}