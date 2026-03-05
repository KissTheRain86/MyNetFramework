using proto.BattleMsg;
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
        public static void MsgMove(ClientState client, MsgMove msg)
        {
            client.RefreshPlayerInfo(msg.x, msg.y, msg.z, client.eulY);
            foreach (ClientState state in NetManager.Clients.Values)
            {
                NetManager.Send(state, msg);
            }
        }

        public static void MsgAttack(ClientState client, MsgAttack msg)
        {
            foreach (ClientState state in NetManager.Clients.Values)
            {
                NetManager.Send(state, msg);
            }
        }
    }
}
