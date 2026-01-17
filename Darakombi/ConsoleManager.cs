using System.Reflection;

namespace Darakombi
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class CommandAttribute : Attribute
    {
        public string CommandName { get; }
        public string Info { get; }

        public CommandAttribute(string n = null, string i = null)
        {
            CommandName = n;
            Info = i;
        }
    }

    public static class ConsoleManager
    {
        public static readonly Dictionary<string, (MethodInfo Method, WeakReference Reference)> Commands = [];

        public static void RegisterStaticCommands()
        {
            var staticCommands = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(a => a.GetCustomAttribute<CommandAttribute>() != null);
            foreach (var command in staticCommands)
            {
                var attribute = command.GetCustomAttribute<CommandAttribute>();
                Commands[attribute.CommandName.ToLower()] = new(command, null);
            }
        }

        public static void RegisterInstanceCommands(object target)
        {
            var instanceCommands = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(a => a.GetCustomAttribute<CommandAttribute>() != null);
            foreach (var command in instanceCommands)
            {
                var attribute = command.GetCustomAttribute<CommandAttribute>();
                Commands[attribute.CommandName.ToLower()] = new(command, new(target));
            }
        }

        public static string Execute(string fullCommand)
        {
            if (string.IsNullOrWhiteSpace(fullCommand)) return string.Empty;

            var tokens = fullCommand.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var commandName = tokens[0].ToLower();
            var args = tokens.Skip(1).ToArray();

            if (!Commands.TryGetValue(commandName, out var command))
            {
                return "Invalid command";
            }

            object target = null;
            if (command.Reference != null)
            {
                if (!command.Reference.IsAlive)
                    Commands.Remove(commandName);
                target = command.Reference.Target;
            }

            var parameters = command.Method.GetParameters();
            //if (parameters.Length < args.Length) // might break custom commands
            //{
            //    return "Arguements";
            //}

            try
            {
                var parsedArgs = new object[parameters.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    var parameterType = parameters[i].ParameterType;
                    parsedArgs[i] = Convert.ChangeType(args[i], parameterType);
                }
                command.Method.Invoke(target, parsedArgs);
                return "Nice";
            }
            catch
            {
                return $"Rip";
            }
        }
    }
}