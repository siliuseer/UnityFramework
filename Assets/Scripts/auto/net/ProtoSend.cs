using siliu.net;
public static class ProtoSend {
	/// <summary> [100] 心跳 </summary>
	public static SendEntry Heart() {
		return new SendProtoEntry(100){ repeat = true };
	}
}