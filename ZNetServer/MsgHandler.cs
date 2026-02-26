using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZNetServer
{
    public class MsgHandler
    {
        public static void MsgEnter(ClientState client, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            float x = float.Parse(split[1]);
            float y = float.Parse(split[2]);
            float z = float.Parse(split[3]);
            float eulY = float.Parse(split[4]);
            client.RefreshPlayerInfo(100, x, y, z, eulY);
            //广播
            string sendStr = "Enter|" + msgArgs;
            foreach (ClientState state in MainClass.Clients.Values)
            {
                MainClass.Send(state, sendStr);
            }
            //Console.WriteLine("MsgEnter" + msgArgs);
        }
        public static void MsgList(ClientState c, string msgArgs)
        {
            string sendStr = "List|";
            foreach (ClientState state in MainClass.Clients.Values)
            {
                sendStr += state.socket.RemoteEndPoint.ToString() + ",";
                sendStr += state.x.ToString() + ",";
                sendStr += state.y.ToString() + ",";
                sendStr += state.z.ToString() + ",";
                sendStr += state.eulY.ToString() + ",";
                sendStr += state.hp.ToString() + ",";
            }
            Console.WriteLine("send:" + sendStr);
            MainClass.Send(c, sendStr);
            // Console.WriteLine("MsgList" + msgArgs);
        }

        public static void MsgMove(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            float x = float.Parse(split[1]);
            float y = float.Parse(split[2]);
            float z = float.Parse(split[3]);
            float eulY = float.Parse(split[4]);
            c.RefreshPlayerInfo(x, y, z, eulY);
            string sendStr = "Move|" + msgArgs;
            foreach (var cs in MainClass.Clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
            //Console.WriteLine("MsgMove" + msgArgs);
        }

        public static void MsgAttack(ClientState c, string msgArgs)
        {
            string sendStr = "Attack|" + msgArgs;
            foreach (var cs in MainClass.Clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
        }

        public static void MsgHit(ClientState c, string msgArgs)
        {
            string[] split = msgArgs.Split(',');
            string attackIp = split[0];
            string hitIp = split[1];
            ClientState hisCs = null;
            foreach (var cs in MainClass.Clients.Values)
            {
                if (cs.socket.RemoteEndPoint?.ToString() == hitIp)
                    hisCs = cs;
                if (hisCs == null) return;
                hisCs.hp -= 25;//test
                //死亡则广播死亡协议 
                if (hisCs.hp <= 0)
                {
                    string sendStr = "Die|" + hisCs.socket.RemoteEndPoint.ToString();
                    foreach (ClientState client in MainClass.Clients.Values)
                    {
                        MainClass.Send(client, sendStr);
                    }
                }
            }
        }
    }
}
