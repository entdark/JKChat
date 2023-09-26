using System.Text;

namespace JKChat.Core.Services {
	public interface IJKClientService {
		Encoding Encoding { get; set; }
		Encoding []AvailableEncodings { get;}
		void SetEncodingById(int id);
	}
}