using System.Text;

using JKClient;

namespace JKChat.Core.Services {
	public class JKClientService : IJKClientService {
		private readonly Encoding []availableEncodings = new Encoding[3] {
			Encoding.UTF8,
			Encoding.GetEncoding(1252),
			Encoding.GetEncoding(1251)
		};
		public Encoding Encoding {
			get => Common.Encoding;
			set {
				Common.Encoding = value;
				Common.AllowAllEncodingCharacters = Common.Encoding.Equals(Encoding.UTF8);
			}
		}
		public Encoding []AvailableEncodings => availableEncodings;
		public void SetEncodingById(int id) {
			if (id < 0 || id >= AvailableEncodings.Length) {
				return;
			}
			Encoding = AvailableEncodings[id];
		}
	}
}