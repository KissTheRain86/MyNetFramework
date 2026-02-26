// Auto Generated. DO NOT EDIT.
using System;
using proto.MsgId;
using System.Collections.Generic;
using ProtoBuf;

public static class MsgRegistry
{
    static MsgRegistry()
    {
        Register<proto.BattleMsg.MsgMove>(MsgId.MsgMove);
        Register<proto.BattleMsg.MsgAttack>(MsgId.MsgAttack);
        Register<proto.SysMsg.MsgPing>(MsgId.MsgPing);
        Register<proto.SysMsg.MsgPong>(MsgId.MsgPong);
    }

    static readonly Dictionary<MsgId, Type> id2Type = new();
    static readonly Dictionary<Type, MsgId> type2Id = new();

    static void Register<T>(MsgId id) where T : IExtensible
    {
        var t = typeof(T);
        id2Type[id] = t;
        type2Id[t] = id;
    }

    public static Type GetType(MsgId id)
        => id2Type.TryGetValue(id, out var t) ? t : null;

    public static MsgId GetId(Type t)
        => type2Id.TryGetValue(t, out var id) ? id : MsgId.None;
}

