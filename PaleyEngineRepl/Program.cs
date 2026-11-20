using PaleyEngine;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

int i = 0;

while (true)
{
    Console.Write("> ");
    var command = Console.ReadLine();

    if(string.IsNullOrWhiteSpace(command))
    {
        continue;
    }   

    switch (command.First())
    {
        case '&':
            RunCount(command[1..]);
            break;
        default:
            Console.WriteLine($"Unknown command: {command}");
            break;
    }
}

static void RunCount(string command)
{
    Assembly a = Assembly.GetExecutingAssembly();

    using Stream? stream = a.GetManifestResourceStream($"PaleyEngineRepl.Samples.{command}.xml");
    if (stream is null)
    {
        throw new PaleyEngineException($"Cannot find schema resource: {command}");
    }
     var document = XDocument.Load(stream);

    var paleyEngine = new PaleyEngine.PaleyEngine();
    paleyEngine.OpenDatabase(@"C:\Users\jonat\source\repos\FreshCount - Original\DB\info.lki");

    paleyEngine.Count(document);
}
