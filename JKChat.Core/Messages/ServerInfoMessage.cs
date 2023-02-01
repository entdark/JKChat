using JKChat.Core.Models;

using MvvmCross.Plugin.Messenger;

namespace JKChat.Core.Messages {
	public class ServerInfoMessage : MvxMessage {
		internal ConnectionStatus Status { get; private set; }
		internal JKClient.ServerInfo ServerInfo { get; private set; }
		internal ServerInfoMessage(object sender, JKClient.ServerInfo serverInfo, ConnectionStatus status) : base(sender) {
			Status = status;
			ServerInfo = serverInfo;
		}
	}
}
