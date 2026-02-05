using proto.BattleMsg;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZNet;

public class Test : MonoBehaviour
{
    private void Awake()
    {
        EventCenter.AddListener<MsgNetConnect>(OnConnectCallback);
    }

    private void Start()
    {
        //±‡¬Î≤‚ ‘
        MsgMove move = new MsgMove();
        move.x = 222;
        byte[] bs = NetManager.Encode(move);
        Debug.Log("Encode MsgMove:" + System.BitConverter.ToString(bs));

        //Ω‚¬Î≤‚ ‘
        ProtoBuf.IExtensible m = NetManager.Decode("proto.BattleMsg.MsgMove", bs, 0, bs.Length);
        MsgMove m2 = (MsgMove)m;
        Debug.Log("Decode MsgMove:" + m2.x);
    }

    private void OnDestroy()
    {
        EventCenter.RemoveListener<MsgNetConnect>(OnConnectCallback);
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
}
