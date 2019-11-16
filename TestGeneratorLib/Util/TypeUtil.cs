using System;

namespace TestGeneratorLib.Util
{
    public static class TypeUtil
    {
        public static object GetDefault(Type type)
        {
            return (type != null && type.IsValueType) ? Activator.CreateInstance(type) : null;
        }
    }
}