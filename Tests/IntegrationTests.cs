using System.Reflection;
using System.Xml.Linq;
using Xunit.Sdk;

namespace Tests;

public class IntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public IntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SimpleAttributesQueryWorks()
    {
        if (!_fixture.DatabaseExists)
        {
            throw SkipException.ForSkip("Required file missing.");
        }

        var xml = """
                  <?xml version="1.0" encoding="utf-8" ?>
                  <Count lev="x" ver="y" date="x">
                  	<generator>PaleyEngineRepl</generator>
                  	<database>gort</database>
                  	<selection>
                  		<attr id="Postcode area" op="=" lev="gort" t="0" l="0">
                  			<value>LS</value> <!-- Leeds 720540 -->
                  			<value>BS</value> <!-- Bristol 762331 -->
                  		</attr>
                  	</selection>
                  </Count>
                  """;


        Assert.Equal(720540 + 762331, GetCount(xml));
    }

    private static long GetCount(string xml)
    {
        var paleyEngine = new PaleyEngine.PaleyEngine();
        paleyEngine.OpenDatabase(DatabaseFixture.Path);

        var document = XDocument.Parse(xml);

        return paleyEngine.Count(document);
    }
}
