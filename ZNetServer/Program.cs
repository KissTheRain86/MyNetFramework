using System.Net;
using System.Net.Sockets;
using ProtoBuf;
using proto.BattleMsg;
using proto.MsgId;
using proto.SysMsg;
using ZNetServer.logic;
using EventHandler = ZNetServer.logic.EventHandler;
using ZNetServer.net;

namespace ZNetServer
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            NetManager.StartLoop(8888);
        }

       

    }
}
