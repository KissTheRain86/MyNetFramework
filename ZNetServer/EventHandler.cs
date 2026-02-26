using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZNetServer
{
    public class EventHandler
    {
        public static void OnDisconnect(ClientState state)
        {
            Console.WriteLine("OnDisconnect");
            if (state == null || state.socket == null)
            {
                Console.WriteLine("state is null");
                return;
            }
            string desc = state.socket.RemoteEndPoint.ToString();
            string sendStr = "Leave|" + desc + ",";
            foreach (var cs in MainClass.Clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
        }
    }
}
