using System.Reflection;

namespace Darakombi
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field)]
    public class DebugWatch : Attribute
    {
        public string Name { get; }
        public DebugWatch(string name = null) => Name = name;
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

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<DebugIgnore>() != null) return;
                var attribute = property.GetCustomAttribute<DebugWatch>();
                if (attribute != null || (trackClass && property.GetMethod?.IsPublic == true))
                    Register(category, attribute?.Name ?? property?.Name, () => property?.GetValue(target)?.ToString());
            }
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.GetCustomAttribute<DebugIgnore>() != null) return;
                var attribute = field.GetCustomAttribute<DebugWatch>();
                if (attribute != null || (trackClass && field.IsPublic))
                    Register(category, attribute.Name ?? field.Name, () => field.GetValue(target)?.ToString());
            }
        }

        public static string GetDebugString(bool includeHeader = false)
        {
            var sb = new StringBuilder();
            foreach (var category in Registry)
            {
                foreach (var item in category.Value)
                {
                    if (item.Active)
                    {
                        if (includeHeader)
                            sb.AppendLine($"-{category.Key}-");
                        sb.AppendLine($"{item.Name}:{item.GetValue.Invoke().ToString()}");
                    }
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