using proto.BattleMsg;
using proto.SysMsg;

namespace ZNetServer;

public class MsgHandler
{
    public static void MsgMove(ClientState client, MsgMove msg)
    {
        client.RefreshPlayerInfo(msg.x, msg.y, msg.z, client.eulY);
        foreach (ClientState state in MainClass.Clients.Values)
        {
            MainClass.Send(state, msg);
        }
    }

    public static void MsgAttack(ClientState client, MsgAttack msg)
    {
        foreach (ClientState state in MainClass.Clients.Values)
        {
            MainClass.Send(state, msg);
        }
    }

    public static void MsgPing(ClientState client, MsgPing _)
    {
        MainClass.Send(client, new MsgPong());
    }
}
