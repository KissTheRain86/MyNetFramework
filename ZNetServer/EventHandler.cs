using System;

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

            Console.WriteLine($"Client disconnected: {state.socket.RemoteEndPoint}");
        }
    }
}
