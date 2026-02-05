using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZNet
{
    public class ByteArray
    {
        //默认大小
        const int DEFAULT_SIZE = 1024;
        //初始大小
        int initSize = 0;
        //缓冲区
        public byte[] Bytes { get; private set; }
        //读写位置
        public int ReadIndex = 0;
        public int WriteIndex = 0;
        //容量
        private int capacity = 0;
        //剩余容量
        public int Remain => capacity - WriteIndex;
        //数据长度 
        public int Length => WriteIndex - ReadIndex;

        public ByteArray(int size = DEFAULT_SIZE)
        {
            Bytes = new byte[size];
            capacity = size;
            initSize = size;
            ReadIndex = 0;
            WriteIndex = 0;
        }

        public ByteArray(byte[] defaultBytes)
        {
            Bytes = defaultBytes;
            capacity = defaultBytes.Length;
            initSize = defaultBytes.Length;
            ReadIndex = 0;
            WriteIndex = defaultBytes.Length;
        }

        public void Resize(int size)
        {
            //避免size过小
            if (size < Length || size < initSize) return;
            int n = 1;
            while (n < size) n *= 2;
            capacity = n;
            byte[] newBytes = new byte[capacity];
            Array.Copy(Bytes, ReadIndex, newBytes, 0, WriteIndex - ReadIndex);
            Bytes = newBytes;
            WriteIndex = Length;
            ReadIndex = 0;
        }

        //写入数据
        public int Write(byte[] bs, int offset, int count)
        {
            if (Remain < count) Resize(Length + count);
            Array.Copy(bs, offset, Bytes, WriteIndex, count);
            WriteIndex += count;
            return count;
        }

        //读取数据
        public int Read(byte[] bs, int offset, int count)
        {
            count = Math.Min(count, Length);
            Array.Copy(Bytes, 0, bs, offset, count);
            ReadIndex += count;
            CheckAndMoveBytes();
            return count;
        }

        public Int16 ReadInt16()
        {
            if (Length < 2) return 0;
            Int16 ret = (Int16)((Bytes[1] << 8) | Bytes[0]);
            ReadIndex += 2;
            CheckAndMoveBytes();
            return ret;
        }

        public Int32 ReadInt32()
        {
            if (Length < 4) return 0;
            Int32 ret = (Int32)((Bytes[3] << 24) |
                (Bytes[2] << 16) |
                (Bytes[1] << 8) |
                Bytes[0]);
            ReadIndex += 4;
            CheckAndMoveBytes();
            return ret;
        }
        private void CheckAndMoveBytes()
        {
            if (Length < 8) MoveBytes();
        }

        private void MoveBytes()
        {
            //移到最前
            Array.Copy(Bytes, ReadIndex, Bytes, 0, Length);
            WriteIndex = Length;
            ReadIndex = 0;
        }
    }
}

