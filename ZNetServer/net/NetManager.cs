using proto.BattleMsg;
using proto.MsgId;
using proto.SysMsg;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Threading.Tasks;
using ZNetServer.logic;
using EventHandler = ZNetServer.logic.EventHandler;

namespace ZNetServer.net
{
    internal class NetManager
    {

        //监听Socket
        public static Socket listenfd;
        //客户端Socket及状态信息
        public static Dictionary<Socket, ClientState> Clients = new();

        //checkReadList
        public static List<Socket> checkReadList = new List<Socket>();

        public static void StartLoop(int listenPort)
        {
            listenfd = new Socket(AddressFamily.InterNetwork,
               SocketType.Stream, ProtocolType.Tcp);
            //bind
            IPAddress ipAddr = IPAddress.Parse("0.0.0.0");
            IPEndPoint ipEdp = new IPEndPoint(ipAddr, listenPort);
            NetManager.listenfd.Bind(ipEdp);
            //listen
            listenfd.Listen(0);
            Console.WriteLine("Start");

            while (true)
            {
                ResetCheckRead();
                //Select 多路复用 一次性检查多个socket是否可读、可写
                Socket.Select(NetManager.checkReadList, null, null, 1000);
                for(int i = checkReadList.Count - 1; i >= 0; i--)
                {
                    Socket s = checkReadList[i];
                    if (s == NetManager.listenfd)
                    {
                        ReadListenfd(s);
                    }
                    else
                    {
                        ReadClientfd(s);
                    }
                }
                //超时
                Timer();
            }
        }

        private static void ReadListenfd(Socket listenfd)
        {
            try
            {
                Socket clientfd = listenfd.Accept();
                Console.WriteLine("Accept" + clientfd.RemoteEndPoint.ToString());
                ClientState state = new ClientState();
                state.socket = clientfd;
                Clients.Add(clientfd, state);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Accept fail" + ex.ToString());
            }
        }

        private static void ReadClientfd(Socket clientfd)
        {
            ClientState state = Clients[clientfd];
            ByteArray readBuff = state.readBuff;
            int count = 0;
            //缓冲区不够， 清除， 若依旧不够， 只能返回
            //缓冲区长度只有 1024, 单条协议超过缓冲区长度时会发生错误， 根据需要调整长度
            if (readBuff.Remain <= 0)
            {
                OnReceiveData(state);
                readBuff.MoveBytes();
            }
            if (readBuff.Remain <= 0)
            {
                Console.WriteLine("Receive fail , maybe msg length > buff capacity");
                Close(state);
                return;
            }
            try
            {
                count = clientfd.Receive(readBuff.Bytes,readBuff.WriteIndex,readBuff.Remain,0);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Receive SocketException " + ex.ToString());
                Close(state);
                return;
            }
            //客户端关闭
            if (count <= 0)
            {
                Console.WriteLine("Socket Close " + clientfd.RemoteEndPoint.ToString());
                Close(state);
                return;
            }
            //消息处理
            readBuff.WriteIndex += count;
            OnReceiveData (state);
            readBuff.CheckAndMoveBytes();
        }

        static void OnReceiveData(ClientState state)
        {
            ByteArray readBuff = state.readBuff;
            if (readBuff.Length < 2)
            {
                Console.WriteLine("Invalid message length");
                return;
            }
            Int16 bodyLen = readBuff.ReadInt16();
            if (readBuff.Length < bodyLen) return;
            //解析协议号

            Int16 msgIdRaw = readBuff.ReadInt16();
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
            using (var ms = new MemoryStream(readBuff.Bytes, 0, bodyLen - 2))
            {
                msg = Serializer.NonGeneric.Deserialize(msgType, ms);
            }
            readBuff.ReadIndex += bodyLen - 2;
            switch (msgId)
            {
                case MsgId.MsgMove:
                    BattleMsgHandler.MsgMove(state, (MsgMove)msg);
                    break;
                case MsgId.MsgAttack:
                    BattleMsgHandler.MsgAttack(state, (MsgAttack)msg);
                    break;
                case MsgId.MsgPing:
                    SysMsgHandler.MsgPing(state, (MsgPing)msg);
                    break;
                default:
                    Console.WriteLine($"Unhandled MsgId:{msgId}");
                    break;
            }
            //继续读取消息
            if (readBuff.Length > 2)
            {
                OnReceiveData(state);
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

        private static void Close(ClientState state)
        {
            EventHandler.OnDisconnect(state);
            state.socket.Close();
            Clients.Remove(state.socket);
            Console.WriteLine("Socket Close");
        }

        public static void ResetCheckRead()
        {
            checkReadList.Clear();
            checkReadList.Add(listenfd);
            foreach(var s in Clients.Values)
            {
                checkReadList.Add(s.socket);
            }
        }


        static void Timer()
        {

        }



    }
}
