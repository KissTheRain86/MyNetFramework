using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZNet
{
    public static class EventCenter
    {
        private static readonly Dictionary<Type, Delegate> _eventTable = new();

        public static void AddListener<T>(Action<T> listener)
        {
            var type = typeof(T);
            if(_eventTable.TryGetValue(type,out var del))
            {
                _eventTable[type] = Delegate.Combine(del, listener);
            }
            else
            {
                _eventTable[type] = listener;
            }
        }

        public static void RemoveListener<T>(Action<T> listener)
        {
            var type = typeof(T);
            if(_eventTable.TryGetValue(type,out var del))
            {
                var curDel = Delegate.Remove(del, listener);
                if (curDel == null)
                {
                    _eventTable.Remove(type);
                }
                else
                {
                    _eventTable[type] = curDel;
                }
            }
        }

        public static void Dispatch<T>(T eventData)
        {
            var type = typeof(T);
            if(_eventTable.TryGetValue(type,out var del))
            {
                ((Action<T>)del)?.Invoke(eventData);
            }
        }
    }

}
