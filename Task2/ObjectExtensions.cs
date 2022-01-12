using System;
using System.Linq;
using System.Reflection;

namespace Task2
{
    public static class ObjectExtensions
    {
        public static void SetReadOnlyProperty(this object obj, string propertyName, object newValue)
        {
            var parentField = obj.GetType().BaseType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(s => s.Name.Contains($"<{propertyName}>", StringComparison.OrdinalIgnoreCase));

            if (parentField != null)
                parentField.SetValue(obj, newValue);

            var field = obj.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(s => s.Name.Contains($"<{propertyName}>", StringComparison.OrdinalIgnoreCase));

            if (field != null)
                field.SetValue(obj, newValue);
        }

        public static void SetReadOnlyField(this object obj, string filedName, object newValue)
        {
            var prop = obj.GetType().GetField(filedName);
            prop.SetValue(obj, newValue);
        }
    }
}
