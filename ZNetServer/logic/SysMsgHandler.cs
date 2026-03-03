using proto.SysMsg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZNetServer.net;

namespace ZNetServer.logic
{
    internal class SysMsgHandler
    {
        public static void MsgPing(ClientState client, MsgPing _)
        {
            NetManager.Send(client, new MsgPong());
        }
    }
}
