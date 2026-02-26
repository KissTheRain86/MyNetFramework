using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using ProtoBuf;
using System.Linq;
using UnityEngine.AI;
using proto.MsgId;
using proto.SysMsg;

namespace ZNet
{
    public enum NetEvent
    {
        ConnectSucc = 1,
        ConnectFail = 2,
        Close = 3,
    }
    public static class NetManager
    {
        static Socket socket;
        //读缓冲区
        static ByteArray readBuff;
        //写入队列
        static Queue<ByteArray> writeQueue;

        static bool isConnecting = false;
        static bool isClosing = false;

        //接收消息列表
        static List<object> msgList = new();
        //接收消息列表长度
        static int msgCount = 0;
        //每一次update处理的消息量
        readonly static int MAX_MESSAG_NUM = 10;

        //心跳
        public static bool IsUsePing = true;
        public static int PingInterval = 30;
        public static readonly int PongTimeOut = 120;
        static float lastPingTime = 0;
        static float lastPongTime = 0;
        private static void InitState()
        {
            socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            readBuff = new ByteArray();
            writeQueue = new Queue<ByteArray>();
            isConnecting = false;
            isClosing = false;
            msgList = new();
            msgCount = 0;

            lastPingTime = Time.time;
            lastPongTime = Time.time;
        }

        public static void Update()
        {
            MsgUpdate();
            PingUpdate();
        }
        public static void UpdatePongTime()
        {
            lastPongTime = Time.time;
        }

        public static void Connect(string ip,int port)
        {
            if(socket!=null && socket.Connected)
            {
                Debug.Log("Connect fail, already connected");
                return;
            }
            if (isConnecting)
            {
                Debug.Log("Connect fail, is connecting");
                return;
            }
            InitState();
            socket.NoDelay = true;
            isConnecting = true;
            socket.BeginConnect(IPAddress.Parse(ip), port, ConnectCallback, socket);
            
        }

        public static void Close()
        {
            if (socket == null || !socket.Connected) return;
            if (isConnecting) return;
            //还有数据发送
            if (writeQueue.Count > 0)
            {
                isClosing = true;
            }
            else
            {
                socket.Close();
                EventCenter.Dispatch(new MsgNetConnect() { state = 3 });
            }
        }

        public static void Send(IExtensible msg)
        {
            if (socket == null || !socket.Connected) return;
            if (isConnecting || isClosing) return;
            //数据编码
            byte[] nameBytes = EncodeName(msg);//两个字节
            byte[] bodyBytes = Encode(msg);
            int len = nameBytes.Length + bodyBytes.Length;
            var sendBytes = new byte[len + 2];
            //组装长度
            sendBytes[0] = (byte)(len % 256);
            sendBytes[1] = (byte)(len / 256);
            //组装协议号
            Array.Copy(nameBytes,0,sendBytes,2,nameBytes.Length);
            //组装消息体
            Array.Copy(bodyBytes, 0, sendBytes, 2 + nameBytes.Length, bodyBytes.Length);

            //写入队列
            ByteArray ba = new ByteArray(sendBytes);
            int count = 0;//writequeue的长度
            lock (writeQueue)
            {
                writeQueue.Enqueue(ba);
                count = writeQueue.Count;
            }
            //发送
            if (count == 1)
            {
                socket.BeginSend(sendBytes, 0, sendBytes.Length,
                    0, SendCallback, socket);
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndConnect(ar);
                Debug.Log("Socket Connect Succ");
                //发送成功连接事件
                EventCenter.Dispatch(new MsgNetConnect { state = 1 });
                isConnecting = false;
                //开始接收数据
                socket.BeginReceive(readBuff.Bytes, readBuff.WriteIndex,
                    readBuff.Remain, 0, ReceiveCallback, socket);
            }catch(SocketException ex)
            {
                Debug.Log("Socket Connect fail" + ex.ToString());
                //发送连接失败事件
                EventCenter.Dispatch(new MsgNetConnect { state = 2 });  
                isConnecting = false;
            }
        }
        private static void SendCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)(ar.AsyncState);
            if (socket == null || !socket.Connected) return;
            //返回实际发送成功的字节数 可能没有把当期消息全部发送完
            int count = socket.EndSend(ar);
            //获取当期正在发送的bytearray
            ByteArray ba = null;
            lock (writeQueue)
            {
                if (writeQueue.Count > 0)
                {
                    ba = writeQueue.First();
                    //记录 刚才发送了count个字节
                    ba.ReadIndex += count;
                    if (ba.Length == 0)
                    {
                        //判断当期协议是否发送完
                        writeQueue.Dequeue();
                        ba = writeQueue.Count>0? writeQueue.First():null;
                    }
                }
            }
            //循环发送
            if (ba != null)
            {
                socket.BeginSend(ba.Bytes, ba.ReadIndex, ba.Length,
                    0, SendCallback, socket);
            }
            else if (isClosing)
            {
                socket.Close();
            }
        }
        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                // 接收数据的字节长度
                int count = socket.EndReceive(ar);
                if (count == 0)
                {
                    Close();
                    return;
                }
                readBuff.WriteIndex += count;
                //处理二进制数据
                OnReceiveData();
                //继续接收数据
                if (readBuff.Remain < 8)
                {
                    readBuff.MoveBytes();
                    readBuff.Resize(readBuff.Length + 2);
                }
                socket.BeginReceive(readBuff.Bytes, readBuff.WriteIndex,
                    readBuff.Remain, 0,ReceiveCallback, socket);

            }catch(SocketException ex)
            {
                Debug.Log("Socket Receive fail:" + ex.ToString());
            }
        }

        private static void OnReceiveData()
        {
            if (readBuff.Length <= 2) return;
            //获取消息体长度
            int readIndex = readBuff.ReadIndex;
            byte[] bytes = readBuff.Bytes;
            Int16 bodyLength = (Int16)((bytes[readIndex + 1] << 8) |
                bytes[readIndex]);
            if (readBuff.Length < bodyLength) return;
            readBuff.ReadIndex += 2;//消息长度
            //解析协议名
            int nameCount = 2;
            Type protoType = DecodeName(readBuff.Bytes,readBuff.ReadIndex);
            if(protoType == null)
            {
                Debug.Log("OnReceiveData Decode Msg Name fail");
                return;
            }
            readBuff.ReadIndex += nameCount;
            //解析协议体
            int bodyCount = bodyLength - nameCount;
            MsgId protoId = MsgRegistry.GetId(protoType);
            var msg = Decode(protoId, readBuff.Bytes, readBuff.ReadIndex, bodyCount);
            readBuff.ReadIndex += bodyCount;
            readBuff.CheckAndMoveBytes();
            //添加到消息队列
            lock (msgList)
            {
                msgList.Add(msg);
            }
            msgCount++;
            //继续读取消息
            if (readBuff.Length > 2)
            {
                OnReceiveData();
            }
        }

        private static void MsgUpdate()
        {
            if (msgCount == 0) return;
            for (int i = 0; i < MAX_MESSAG_NUM; i++)
            {
                //获取第一条消息
                object msg = null;
                lock (msgList)
                {
                    if (msgList.Count > 0)
                    {
                        msg = msgList[0];
                        msgList.RemoveAt(0);
                        msgCount--;
                    }
                }
                //分发消息
                if (msg != null)
                {
                    MsgId msgId = MsgRegistry.GetId(msg.GetType());
                    EventCenter.Dispatch<MsgNetProto>(new MsgNetProto {Msg = msg,MsgId = msgId });
                }else
                {
                    break;
                }
            }
        }

        //心跳
        private static void PingUpdate()
        {
            if (IsUsePing == false) return;
            // send ping
            if(Time.time - lastPingTime > PingInterval)
            {
                MsgPing msgPing = new MsgPing();
                Send(msgPing);
                lastPingTime = Time.time;
            }
            //检测pong时间 超时关闭
            if(Time.time - lastPongTime > PongTimeOut)
            {
                Close();
            }
        }

       
        #region encode and decode

        public static byte[] Encode(IExtensible msg)
        {
            using(var memory = new System.IO.MemoryStream())
            {
                Serializer.Serialize(memory, msg);
                return memory.ToArray();
            }
        }

        //两个字节存放协议号
        public static byte[] EncodeName(IExtensible msg)
        {
            int msgId = (int)MsgRegistry.GetId(msg.GetType());
            //转成ushort两字节
            ushort id = (ushort)msgId;
            //小端写入
            byte[] bytes = new byte[2];
            bytes[0] = (byte)(id & 0xFF);//取低八位
            bytes[1] = (byte)((id >> 8) & 0xFF);//取高八位
            return bytes;
        }

        public static object Decode(MsgId protoId,
            byte[] bytes,int offset,int count)
        {
            using(var memory = new System.IO.MemoryStream(bytes, offset, count))
            {
                Type t = MsgRegistry.GetType(protoId);
                if (t == null)
                {
                    Debug.LogError("Decode: protoId is not exist");
                    return null;
                }
                return Serializer.NonGeneric.Deserialize(t, memory);
            }
        }

        public static Type DecodeName(byte[] bytes, int offset)
        {
            if (bytes == null || bytes.Length < offset + 2)
            {
                Debug.LogError("DecodeName: bytes length not enough");
                return null;
            }
            //读取两个字节（小端）
            ushort msgId = (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
            //转成枚举
            if (!Enum.IsDefined(typeof(MsgId), msgId))
            {
                Debug.LogError($"DecodeName: Unknown MsgId = {msgId}");
                return null;
            }
            return MsgRegistry.GetType((MsgId)msgId);
        }


        #endregion


    }
}

