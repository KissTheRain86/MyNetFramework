using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ByteArray 
{
    //默认大小
    const int DEFAULT_SIZE = 1024;
    //初始大小
    int initSize = 0;
    //缓冲区
    public byte[] Bytes {  get; private set; }
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
        if (size < Length || size<initSize) return;
        int n = 1;
        while (n < size) n *= 2;
        capacity = n;
        byte[] newBytes = new byte[capacity];
        Array.Copy(Bytes, ReadIndex, newBytes, 0, WriteIndex - ReadIndex);
        Bytes = newBytes;
        WriteIndex = Length;
        ReadIndex = 0;
    }
}
