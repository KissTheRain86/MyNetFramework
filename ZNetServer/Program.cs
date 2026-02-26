using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace ZNetServer
{
    class MainClass
    {
        //监听Socket
        static Socket listenfd;
        //客户端Socket及状态信息
        public static Dictionary<Socket, ClientState> Clients = new();

        //checkReadList
        static List<Socket> checkReadList = new List<Socket>();
        public static void Main(string[] args)
        {
            // 建立连接socket          
            listenfd = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            //bind
            IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
            IPEndPoint ipEdp = new IPEndPoint(ipAddr, 8888);
            listenfd.Bind(ipEdp);
            //listen
            listenfd.Listen(0);
            Console.WriteLine("Server Start");

            while (true)
            {
                //填充checkRead列表
                checkReadList.Clear();
                checkReadList.Add(listenfd);
                foreach (ClientState s in Clients.Values)
                {
                    checkReadList.Add(s.socket);
                }
                //Select 多路复用 一次性检查多个socket是否可读、可写
                Socket.Select(checkReadList, null, null, 1000);
                foreach (Socket s in checkReadList)
                {
                    if (s == listenfd)
                    {
                        //负责监听的socket
                        //有新的客户端连接 
                        ReadListenfd(s);
                    }
                    else
                    {
                        //负责读的socket
                        ReadClientfd(s);
                    }
                }
            }
        }

        public static void ReadListenfd(Socket listenfd)
        {
            Console.WriteLine("Server Accept");
            Socket clientfd = listenfd.Accept();
            ClientState state = new ClientState();
            state.socket = clientfd;
            Clients.Add(clientfd, state);
        }

        public static bool ReadClientfd(Socket clientfd)
        {
            ClientState state = Clients[clientfd];
            //接收
            int count = 0;
            try
            {
                count = clientfd.Receive(state.readBuff);
            }
            catch (SocketException ex)
            {
                OnClientDisconnect(state, clientfd);
                return false;
            }
            //客户端关闭
            if (count <= 0)
            {
                OnClientDisconnect(state, clientfd);
                return false;
            }
            //将收到的数据追加到缓存
            state.cache.AddRange(state.readBuff.AsSpan(0, count).ToArray());

            //拆包 可能一次recieve拿到多条
            while (true)
            {
                //长度不足消息头 
                if (state.cache.Count < 2) break;
                Int16 bodyLen = BitConverter.ToInt16(state.cache.ToArray(), 0);
                //消息不完整
                if (state.cache.Count < 2 + bodyLen) break;
                //完整 取出消息体
                byte[] bodyBytes = state.cache.GetRange(2, bodyLen).ToArray();
                //移除已处理数据
                state.cache.RemoveRange(0, 2 + bodyLen);
                //处理消息 
                string recvStr = System.Text.Encoding.UTF8.GetString(bodyBytes);
                HandleMsg(state, recvStr);
            }
            return true;
        }

        static void HandleMsg(ClientState state, string recvStr)
        {
            Console.WriteLine("Recieve:" + recvStr);

            string[] split = recvStr.Split('|');
            if (split.Length < 2)
            {
                Console.WriteLine("Invalid message format");
                return;
            }
            string msgName = split[0];
            string msgArgs = split[1];
            string funcName = "Msg" + msgName;
            MethodInfo methodInfo = typeof(MsgHandler).GetMethod(funcName);
            if (methodInfo == null)
            {
                Console.WriteLine("cant find method:" + funcName);
                return;
            }
            object[] o = { state, msgArgs };
            methodInfo.Invoke(null, o);
        }

        public static void Send(ClientState state, string sendStr)
        {
            byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(sendStr);//请求体
            Int16 len = (Int16)bodyBytes.Length;
            byte[] lenBytes = BitConverter.GetBytes(len);//长度标识
            byte[] sendBytes = lenBytes.Concat(bodyBytes).ToArray();
            state.socket.Send(sendBytes);
        }
        private static void OnClientDisconnect(ClientState state, Socket clientfd)
        {
            //发送该玩家断线的事件
            MethodInfo info = typeof(EventHandler).GetMethod("OnDisconnect");
            object[] args = { state };
            info.Invoke(null, args);

            //移除该玩家的socket
            clientfd.Close();
            Clients.Remove(clientfd);
            Console.WriteLine("Socket Close");
        }

    }

}