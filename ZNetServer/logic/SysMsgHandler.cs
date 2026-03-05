using proto.SysMsg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZNetServer.net;

namespace ZNetServer.logic
{
    public partial class MsgHandler
    {
        public static void MsgPing(ClientState client, MsgPing _)
        {
            Console.WriteLine("MsgPing");
            client.lastPingTime = NetManager.GetTimeStamp();
            NetManager.Send(client, new MsgPong());
        }
    }
}
