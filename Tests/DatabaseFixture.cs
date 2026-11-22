using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Tests;

[method: SuppressMessage(
        "Major Code Smell",
        "S1118:Utility classes should not have public constructors",
        Justification = "Static utility class pattern is intentional.")]
public class DatabaseFixture()
{
    //public const string Path = @"C:\Users\jonat\source\repos\FreshCount - Original\DB\info.lki";
    public const string Path = @"C:\Databases\Test\lk\info.lki";

    public bool DatabaseExists { get; } = File.Exists(Path);
}
