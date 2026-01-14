using System.Reflection;

namespace Darakombi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public class DebugWatch : Attribute
    {
        public string Name { get; }
        public string Format { get; }
        public DebugWatch(string n = null, string f = null)
        {
            Name = n;
            Format = f;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DebugIgnore : Attribute { }

    public static class DebugManager
    {
        public class DebugItem
        {
            public string Name { get; set; }
            public Func<string> GetValue { get; set; }
            public bool Active { get; set; } = false;
        }

        public readonly static Dictionary<string, List<DebugItem>> Registry = [];
        public static event Action OnRegistryChanged;

        public static void Register(string category, string name, Func<string> value)
        {
            if (!Registry.TryGetValue(category, out var items))
            {
                items = [];
                Registry[category] = items;
            }

            if (!items.Any(x => x.Name == name))
            {
                items.Add(new DebugItem
                {
                    Name = name,
                    GetValue = () => value?.Invoke() ?? "null",
                });
                OnRegistryChanged?.Invoke();
            }
        }

        public static void Track(object target, string category = null)
        {
            if (target == null) return;

            var type = target.GetType();
            category ??= type.Name;
            bool trackClass = type.GetCustomAttribute<DebugWatch>() != null;

            Register(category, "!ToStr", () => target.ToString());

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<DebugIgnore>() != null) return;
                var attribute = property.GetCustomAttribute<DebugWatch>();
                if (attribute != null || (trackClass && property.GetMethod?.IsPublic == true))
                {
                    var name = attribute?.Name ?? property?.Name;
                    var format = attribute?.Format;

                    string getValue()
                    {
                        var value = property.GetValue(target);
                        if (value == null) return "null";

                        if (!string.IsNullOrEmpty(format) && value is IFormattable valFormattable)
                            return valFormattable.ToString(format, null);
                        return value.ToString();
                    }
                    Register(category, name, getValue);
                }
            }
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<DebugIgnore>() != null) return;
                var attribute = field.GetCustomAttribute<DebugWatch>();
                if (attribute != null || (trackClass && field.IsPublic == true))
                {
                    var name = attribute?.Name ?? field?.Name;
                    var format = attribute?.Format;

                    string getValue()
                    {
                        var value = field.GetValue(target);
                        if (value == null) return "null";

                        if (!string.IsNullOrEmpty(format) && value is IFormattable valFormattable)
                            return valFormattable.ToString(format, null);
                        return value.ToString();
                    }
                    Register(category, name, getValue);
                }
            }
        }

        public static string GetDebugString(bool includeCategoryHeader = true)
        {
            var sb = new StringBuilder();
            foreach (var category in Registry)
            {
                if (includeCategoryHeader && category.Value.Any(x => x.Active))
                    sb.AppendLine($"{category.Key}:");
                foreach (var item in category.Value)
                {
                    if (item.Active)
                        sb.AppendLine($"{item.Name}:{item.GetValue.Invoke().ToString()}");
                }
            }
            return sb.ToString();
        }

        public static void Clear()
        {
            Registry?.Clear();
            OnRegistryChanged?.Invoke();
        }
    }
}