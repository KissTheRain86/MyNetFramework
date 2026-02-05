using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using ProtoBuf;
using System.Linq;

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

        private static void InitState()
        {
            socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            readBuff = new ByteArray();
            writeQueue = new Queue<ByteArray>();
            isConnecting = false;
            isClosing = false;
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
            var sendBytes = Encode(msg);
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

        #region encode and decode

        public static byte[] Encode(IExtensible msg)
        {
            using(var memory = new System.IO.MemoryStream())
            {
                Serializer.Serialize(memory, msg);
                return memory.ToArray();
            }
        }

        public static IExtensible Decode(string protoName,
            byte[] bytes,int offset,int count)
        {
            using(var memory = new System.IO.MemoryStream(bytes, offset, count))
            {
                Type t = Type.GetType(protoName);
                return (IExtensible)Serializer.NonGeneric.Deserialize(t, memory);
            }
        }
        #endregion


    }
}

