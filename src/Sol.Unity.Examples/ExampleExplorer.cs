﻿using System;
using System.Linq;
using System.Reflection;

namespace Sol.Unity.Examples
{
    public class ExampleExplorer
    {
        public static void Main(string[] args)
        {
            var examples = Assembly.GetEntryAssembly().GetExportedTypes().Where(t => typeof(IExample).IsAssignableFrom(t)).ToList();

            while (true)
            {
                Console.WriteLine("Choose an example to run: ");
                int i = 0;
                foreach (var ex in examples)
                {
                    Console.WriteLine($"{i++}){ex.Name}");
                }

                var option = Console.ReadLine();

                if (int.TryParse(option, out int res) && res <= examples.Count && res >= 0)
                {
                    var t = examples[res];
                    var example = (IExample)t.GetConstructor(Type.EmptyTypes).Invoke(null);

                    example.Run();
                }
                else
                {
                    Console.WriteLine("invalid option");
                }
            }

        }
    }
}