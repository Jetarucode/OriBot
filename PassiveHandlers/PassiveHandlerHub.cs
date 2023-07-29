using System;
using System.Collections.Generic;
using System.Reflection;

using Discord.WebSocket;

namespace OriBot.PassiveHandlers
{
    public static class PassiveHandlerHub
    {
        private static readonly List<Type> _passiveHandlers = new();
        private static readonly Dictionary<Type, List<MethodInfo>> _passiveHandlersToMethods = new();

        /// <summary>
        /// This method will register all passive handlers, to <paramref name="client"/>.MessageReceived
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool RegisterPassiveHandlers(DiscordSocketClient client)
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(BasePassiveHandler)) && !type.IsAbstract)
                {
                    _passiveHandlers.Add(type);
                    var methods = new List<MethodInfo>();
                    foreach (var method in type.GetMethods())
                    {
                        if (method.GetCustomAttribute<PassiveHandler>() != null)
                        {
                            methods.Add(method);
                        }
                    }
                    _passiveHandlersToMethods.Add(type, methods);
                }
            }
            client.MessageReceived += async (message) =>
            {
                RunPassiveHandlers(client, message);
            };

            return true;
        }

        private static void RunPassiveHandlers(DiscordSocketClient client, SocketMessage message)
        {
            foreach (var item in _passiveHandlersToMethods)
            {
                // First, we run the RequirementEngine
                var instantiated = (BasePassiveHandler)Activator.CreateInstance(item.Key, args: new object[] { client, message });
                var result = instantiated.GetRequirements().CheckRequirements(client, message);
                if (!result) continue;
                foreach (var method in item.Value)
                {
                    method.Invoke(instantiated, null);
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PassiveHandler : Attribute
    {
        public PassiveHandler()
        { }
    }
}