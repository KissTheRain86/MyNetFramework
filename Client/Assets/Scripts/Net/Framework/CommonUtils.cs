using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CommonUtils
{
    /// <summary>
    /// int转 enum
    /// </summary>
    public static string _ParseEnumName(this int value,Type enumType)
    {
        if (enumType == null)
            throw new ArgumentNullException(nameof(enumType));

        if (!enumType.IsEnum)
            throw new ArgumentException("Type must be an enum");
        if (Enum.IsDefined(enumType, value))
        {
            return Enum.GetName(enumType, value);
        }
        return null;
    }

    /// <summary>
    /// enum 名称转 int
    /// </summary>
    public static bool _TryParseEnumValue(this string name, Type enumType, out int value)
    {
        value = default;

        if (string.IsNullOrEmpty(name))
            return false;

        if (enumType == null || !enumType.IsEnum)
            return false;

        if (Enum.TryParse(enumType, name, out object enumObj))
        {
            value = Convert.ToInt32(enumObj);
            return true;
        }

        return false;
    }
}
