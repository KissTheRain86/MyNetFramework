using proto.BattleMsg;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZNet;
using proto.MsgId;

public class Test : MonoBehaviour
{
    private void Awake()
    {
        EventCenter.AddListener<MsgNetConnect>(OnConnectCallback);
        EventCenter.AddListener<MsgNetProto>(OnProtoCallback);
    }

    private void Start()
    {
        //编码测试
        MsgMove move = new MsgMove();
        move.x = 222;
        byte[] bs = NetManager.Encode(move);
        Debug.Log("Encode MsgMove:" + System.BitConverter.ToString(bs));

        //解码测试
        object m = NetManager.Decode(MsgId.MsgMove, bs, 0, bs.Length);
        MsgMove m2 = (MsgMove)m;
        Debug.Log("Decode MsgMove:" + m2.x);
    }

    private void Update()
    {
        NetManager.Update();
    }

    private void OnDestroy()
    {
        EventCenter.RemoveListener<MsgNetConnect>(OnConnectCallback);
        EventCenter.RemoveListener<MsgNetProto>(OnProtoCallback);
    }

    public void OnConnectClick()
    {
        NetManager.Connect("127.0.0.1", 8888);
        
    }

    public void OnCloseClick()
    {
        NetManager.Close();
    }

    private void OnConnectCallback(MsgNetConnect data)
    {
        switch (data.state)
        {
            case 1:
                Debug.Log("OConnectSucc");
                break;
            case 2:
                Debug.Log("OnConnectFail");
                break;
            case 3:
                Debug.Log("OnConnectClose");
                break;
        }
      
    }

    private void OnProtoCallback(MsgNetProto data)
    {
        var proto = data.Msg;
        if(proto!=null)
        {
            switch (data.MsgId)
            {
                case MsgId.MsgMove:
                    MsgMove moveProto = (MsgMove)proto;
                    Debug.Log("OnMsgMove msg.x=" + moveProto.x);
                    break;
            }
        }
    }
}
