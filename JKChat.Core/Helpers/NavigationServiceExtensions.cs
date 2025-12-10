using System.Collections.Generic;
using System.Threading.Tasks;
using JKChat.Core.Navigation;

namespace JKChat.Core.Helpers;

public static class NavigationServiceExtensions {
	private const string ChatHost = "chat";
	private const string ServerInfoHost = "info";
	public static Task<bool> NavigateToChat(this INavigationService navigationService, string address) {
		return navigationService.NavigateToHost(ChatHost, address);
	}
	public static Task<bool> NavigateToServerInfo(this INavigationService navigationService, string address) {
		return navigationService.NavigateToHost(ServerInfoHost, address);
	}
	public static IDictionary<string, string> MakeChatNavigationParameters(this INavigationService navigationService, string address) {
		return navigationService.MakeNavigationParametersForHost(ChatHost, address);
	}
	public static IDictionary<string, string> MakeServerInfoNavigationParameters(this INavigationService navigationService, string address) {
		return navigationService.MakeNavigationParametersForHost(ServerInfoHost, address);
	}
	private static async Task<bool> NavigateToHost(this INavigationService navigationService, string host, string address) {
		var parameters = navigationService.MakeNavigationParametersForHost(host, address);
		return await navigationService.Navigate(parameters);
	}
	private static IDictionary<string, string> MakeNavigationParametersForHost(this INavigationService navigationService, string host, string address) {
		return navigationService.MakeNavigationParameters($"jkchat://{host}?address={address}", address);
	}
}