using System;
using System.Collections.Generic;
using ProtoBuf;
using proto.MsgId;

namespace ZNetServer;

public static class MsgRegistry
{
    static MsgRegistry()
    {
        Register<proto.BattleMsg.MsgMove>(MsgId.MsgMove);
        Register<proto.BattleMsg.MsgAttack>(MsgId.MsgAttack);
        Register<proto.SysMsg.MsgPing>(MsgId.MsgPing);
        Register<proto.SysMsg.MsgPong>(MsgId.MsgPong);
    }

    private static readonly Dictionary<MsgId, Type> Id2Type = new();
    private static readonly Dictionary<Type, MsgId> Type2Id = new();

    private static void Register<T>(MsgId id) where T : IExtensible
    {
        var t = typeof(T);
        Id2Type[id] = t;
        Type2Id[t] = id;
    }

    public static Type? GetType(MsgId id)
        => Id2Type.TryGetValue(id, out var t) ? t : null;

    public static MsgId GetId(Type t)
        => Type2Id.TryGetValue(t, out var id) ? id : MsgId.None;
}
