using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

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


        #region encode and decode

        public static byte[] Encode(ProtoBuf.IExtensible msg)
        {
            using(var memory = new System.IO.MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(memory, msg);
                return memory.ToArray();
            }
        }

        public static ProtoBuf.IExtensible Decode(string protoName,
            byte[] bytes,int offset,int count)
        {
            using(var memory = new System.IO.MemoryStream(bytes, offset, count))
            {
                Type t = Type.GetType(protoName);
                return (ProtoBuf.IExtensible)ProtoBuf.Serializer.NonGeneric.Deserialize(t, memory);
            }
        }
        #endregion


    }
}

