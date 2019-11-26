using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SabberStoneContract.Interface;

namespace SabberStonePython.DotnetAIService
{
    public static class FindAI
    {
        private const string AI_DIR = "ai";

        private static Assembly[] FindAssembly()
        {
            // Get all *.dll files
            string[] referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
            
            // Get assemblies
            Assembly[] assemblies = referencedPaths
                .Where(path => !path.Contains("SabberStone")) // Exclude common assemblies (Probably need refactoring, ad hoc)
                .Select(path => AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)))
                .ToArray();

            Console.WriteLine("***** FindAssembly(): ");
            foreach (Assembly a in assemblies)
                Console.WriteLine(a.GetName());
            Console.WriteLine("*****");

            return assemblies;
        }

        private static Type[] GetAIAgentTypes(Assembly[] assemblies)
        {
            Type aiInterface = typeof(IGameAI);

            Type[] agents = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsInterface && 
                               !type.IsAbstract &&
                               type.GetInterfaces().Contains(aiInterface))
                .ToArray();

            Console.WriteLine("***** GetAIAgentTypes(): ");
            foreach (Type agent in agents)
                Console.WriteLine(agent.FullName);
            Console.WriteLine("*****");

            return agents;
        }

        private static Type[] GetAITypes()
        {
            Assembly[] assemblies = FindAssembly();
            Type[] agents = GetAIAgentTypes(assemblies);
            return agents;
        }

        private static readonly Lazy<Type[]> AIs = new Lazy<Type[]>(GetAITypes);

        public static IGameAI GetAI(string name)
        {
            Type[] ais = AIs.Value;
            foreach (Type aiType in ais)
                if (aiType.AssemblyQualifiedName.Contains(name, StringComparison.InvariantCultureIgnoreCase))
                    return (IGameAI) Activator.CreateInstance(aiType);

            throw new Exception("Can't find an ai agent of name " + name);
        }
    }
}
