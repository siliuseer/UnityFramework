using System.Collections.Generic;
using siliu.net;
public static class ProtoReceive
{
	public static List<IDownCfg> cfgs = new List<IDownCfg>
	{
		// 心跳
		new DownCfg<Proto_HeartDown> { proto = 100 },
	};
}