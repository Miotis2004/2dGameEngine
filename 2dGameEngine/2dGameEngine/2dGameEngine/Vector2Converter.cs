using System;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;

namespace _2dGameEngine;

public class Vector2Converter : ExpandableObjectConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        if (sourceType == typeof(string))
            return true;
        return base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            string[] parts = str.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && float.TryParse(parts[0], NumberStyles.Float, culture, out float x) && float.TryParse(parts[1], NumberStyles.Float, culture, out float y))
            {
                return new Vector2(x, y);
            }
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Vector2 v)
        {
            return $"{v.X}, {v.Y}";
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }

    public override bool GetCreateInstanceSupported(ITypeDescriptorContext? context) => true;

    public override object CreateInstance(ITypeDescriptorContext? context, System.Collections.IDictionary propertyValues)
    {
        return new Vector2((float)(propertyValues["X"] ?? 0f), (float)(propertyValues["Y"] ?? 0f));
    }
}
