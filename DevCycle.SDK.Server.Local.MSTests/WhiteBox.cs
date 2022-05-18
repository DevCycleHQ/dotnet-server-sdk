using System;
using System.Reflection;

namespace DevCycle.SDK.Server.Local.MSTests
{
    public static class WhiteBox
    {
        public static void SetPrivateFieldValue<T>(this object obj, string propName, T val)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            Type t = obj.GetType();
            FieldInfo fi = null;

            while (fi == null && t != null)
            {
                fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                t = t.BaseType;
            }

            if (fi == null)
            {
                throw new ArgumentOutOfRangeException("propName", string.Format("Field {0} was not found in Type {1}", propName, obj.GetType().FullName));
            }

            fi.SetValue(obj, val);
        }
    }
}
