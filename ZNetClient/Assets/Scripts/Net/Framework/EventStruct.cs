using proto.MsgId;
using ProtoBuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZNet
{
    public struct MsgNetConnect
    {
        public int state;//1成功 2失败 3关闭
    }

    public struct MsgNetProto
    {       
        public MsgId MsgId;
        public object Msg;
    }

}