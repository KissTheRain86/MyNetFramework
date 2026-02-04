using System.Collections;
using System.Collections.Generic;
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
    }
}

