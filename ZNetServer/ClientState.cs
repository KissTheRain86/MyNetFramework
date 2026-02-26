using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ZNetServer
{
    public class ClientState
    {
        public Socket socket;
        public byte[] readBuff = new byte[1024];

        //新增缓存 解决粘包/拆包
        public List<byte> cache = new List<byte>();
        // player data
        public int hp = -100;
        public float x = 0;
        public float y = 0;
        public float z = 0;
        public float eulY = 0;

        public void RefreshPlayerInfo(int hp, float x, float y, float z, float eulY)
        {
            this.hp = hp;
            this.x = x;
            this.y = y;
            this.z = z;
            this.eulY = eulY;
        }

        public void RefreshPlayerInfo(float x, float y, float z, float eulY)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.eulY = eulY;
        }


    }
}
