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
