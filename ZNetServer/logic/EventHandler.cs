using System;
using ZNetServer.net;

namespace ZNetServer.logic
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

            Console.WriteLine($"Client disconnected: {state.socket.RemoteEndPoint}");
        }

        public static void OnTimer()
        {
            CheckPing();
        }

        //ping检查
        private static List<ClientState> _pingCloseList = new List<ClientState>();
        private static void CheckPing()
        {
            long timeNow = NetManager.GetTimeStamp();
            _pingCloseList.Clear();

            foreach (var client in NetManager.Clients.Values)
            {
                if (timeNow - client.lastPingTime > NetManager.MaxPingInterval)
                {
                    _pingCloseList.Add(client);
                }
            }

            foreach (var client in _pingCloseList)
            {
                Console.WriteLine("Ping Close " + client.socket.RemoteEndPoint);
                NetManager.Close(client);
            }
        }



    }

   
}
