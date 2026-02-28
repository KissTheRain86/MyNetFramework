using System.Net;
using System.Net.Sockets;
using ProtoBuf;
using proto.BattleMsg;
using proto.MsgId;
using proto.SysMsg;

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
                        ReadListenfd(s);
                    }
                    else
                    {
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
            int count = 0;
            try
            {
                count = clientfd.Receive(state.readBuff);
            }
            catch (SocketException)
            {
                OnClientDisconnect(state, clientfd);
                return false;
            }

            if (count <= 0)
            {
                OnClientDisconnect(state, clientfd);
                return false;
            }

            state.cache.AddRange(state.readBuff.AsSpan(0, count).ToArray());

            while (true)
            {
                if (state.cache.Count < 2) break;
                Int16 bodyLen = BitConverter.ToInt16(state.cache.ToArray(), 0);
                if (state.cache.Count < 2 + bodyLen) break;

                byte[] bodyBytes = state.cache.GetRange(2, bodyLen).ToArray();
                state.cache.RemoveRange(0, 2 + bodyLen);
                HandleMsg(state, bodyBytes);
            }
            return true;
        }

        static void HandleMsg(ClientState state, byte[] bodyBytes)
        {
            if (bodyBytes.Length < 2)
            {
                Console.WriteLine("Invalid message length");
                return;
            }

            ushort msgIdRaw = (ushort)(bodyBytes[0] | (bodyBytes[1] << 8));
            if (!Enum.IsDefined(typeof(MsgId), (int)msgIdRaw))
            {
                Console.WriteLine($"Unknown MsgId:{msgIdRaw}");
                return;
            }

            MsgId msgId = (MsgId)msgIdRaw;
            Type? msgType = MsgRegistry.GetType(msgId);
            if (msgType == null)
            {
                Console.WriteLine($"MsgId not registered:{msgId}");
                return;
            }

            object msg;
            using (var ms = new MemoryStream(bodyBytes, 2, bodyBytes.Length - 2))
            {
                msg = Serializer.NonGeneric.Deserialize(msgType, ms);
            }

            switch (msgId)
            {
                case MsgId.MsgMove:
                    MsgHandler.MsgMove(state, (MsgMove)msg);
                    break;
                case MsgId.MsgAttack:
                    MsgHandler.MsgAttack(state, (MsgAttack)msg);
                    break;
                case MsgId.MsgPing:
                    MsgHandler.MsgPing(state, (MsgPing)msg);
                    break;
                default:
                    Console.WriteLine($"Unhandled MsgId:{msgId}");
                    break;
            }
        }

        public static void Send(ClientState state, IExtensible msg)
        {
            byte[] bodyBytes;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, msg);
                bodyBytes = ms.ToArray();
            }

            ushort msgId = (ushort)MsgRegistry.GetId(msg.GetType());
            byte[] msgIdBytes = new byte[2];
            msgIdBytes[0] = (byte)(msgId & 0xFF);
            msgIdBytes[1] = (byte)((msgId >> 8) & 0xFF);

            int payloadLen = msgIdBytes.Length + bodyBytes.Length;
            byte[] sendBytes = new byte[payloadLen + 2];
            sendBytes[0] = (byte)(payloadLen % 256);
            sendBytes[1] = (byte)(payloadLen / 256);
            Array.Copy(msgIdBytes, 0, sendBytes, 2, msgIdBytes.Length);
            Array.Copy(bodyBytes, 0, sendBytes, 4, bodyBytes.Length);

            state.socket.Send(sendBytes);
        }

        private static void OnClientDisconnect(ClientState state, Socket clientfd)
        {
            EventHandler.OnDisconnect(state);
            clientfd.Close();
            Clients.Remove(clientfd);
            Console.WriteLine("Socket Close");
        }

    }
}
