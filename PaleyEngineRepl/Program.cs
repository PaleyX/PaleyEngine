using PaleyEngine;
using System.Reflection;
using System.Xml.Linq;

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
    var a = Assembly.GetExecutingAssembly();

    using var stream = a.GetManifestResourceStream($"PaleyEngineRepl.Samples.{command}.xml");
    if (stream is null)
    {
        throw new PaleyEngineException($"Cannot find schema resource: {command}");
    }

    var document = XDocument.Load(stream);

    var paleyEngine = new PaleyEngine.PaleyEngine();
    //paleyEngine.OpenDatabase(@"C:\Users\jonat\source\repos\FreshCount - Original\DB\info.lki");
    paleyEngine.OpenDatabase(@"C:\Databases\Test\lk\info.lki");

    var count = paleyEngine.Count(document);
    Console.WriteLine($"Count: {count:N0}");
}
