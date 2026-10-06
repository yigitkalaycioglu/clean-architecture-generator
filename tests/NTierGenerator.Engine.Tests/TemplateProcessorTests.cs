using NTierGenerator.Engine.Templating;

namespace NTierGenerator.Engine.Tests;

public class TemplateProcessorTests
{
    private static TemplateContext CreateContext(bool auth = true, bool sample = true) =>
        TemplateContext.Create(new GeneratorOptions
        {
            SolutionName = "Acme.Shop",
            OutputDirectory = "C:\\temp",
            IncludeAuthentication = auth,
            IncludeSampleModule = sample
        });

    [Fact]
    public void Process_ReplacesKnownTokensAndKeepsUnknownOnes()
    {
        var result = TemplateProcessor.Process("namespace __Name__.Core; // __pycache__ __Unknown__", "x.txt", CreateContext());

        Assert.Equal("namespace Acme.Shop.Core; // __pycache__ __Unknown__\r\n", result);
    }

    [Theory]
    [InlineData(true, "A\r\nB\r\n")]
    [InlineData(false, "A\r\nC\r\n")]
    public void Process_AppliesIfElse(bool auth, string expected)
    {
        const string template = "A\n//#if Auth\nB\n//#else\nC\n//#endif\n";

        Assert.Equal(expected, TemplateProcessor.Process(template, "x.txt", CreateContext(auth: auth)));
    }

    [Fact]
    public void Process_SupportsNestedBlocksAndAllMarkerStyles()
    {
        const string template = """
            start
            <!--#if Sample-->
            sample
            @*#if !Auth*@
            sample-without-auth
            @*#endif*@
            <!--#endif-->
            end
            """;

        var withAuth = TemplateProcessor.Process(template, "x.txt", CreateContext(auth: true, sample: true));
        var withoutAuth = TemplateProcessor.Process(template, "x.txt", CreateContext(auth: false, sample: true));

        Assert.Equal("start\r\nsample\r\nend\r\n", withAuth);
        Assert.Equal("start\r\nsample\r\nsample-without-auth\r\nend\r\n", withoutAuth);
    }

    [Fact]
    public void Process_SupportsElif()
    {
        const string template = "//#if Sqlite\nsqlite\n//#elif SqlServer\nsqlserver\n//#else\nother\n//#endif\n";

        Assert.Equal("sqlserver\r\n", TemplateProcessor.Process(template, "x.txt", CreateContext()));
    }

    [Fact]
    public void Process_CSharpFile_RemovesBlankLinesLeftByRemovedBlocks()
    {
        const string template = "class A\n{\n//#if Auth\n    int B;\n\n//#endif\n}\n";

        Assert.Equal("class A\r\n{\r\n}\r\n", TemplateProcessor.Process(template, "A.cs", CreateContext(auth: false)));
    }

    [Theory]
    [InlineData("//#if Auht\nx\n//#endif\n")]
    [InlineData("//#if Auth\nx\n")]
    [InlineData("x\n//#endif\n")]
    [InlineData("//#if Auth &&\nx\n//#endif\n")]
    public void Process_InvalidDirectives_Throw(string template)
    {
        Assert.Throws<TemplateException>(() => TemplateProcessor.Process(template, "x.txt", CreateContext()));
    }

    [Theory]
    [InlineData("Auth", true)]
    [InlineData("!Auth", false)]
    [InlineData("Auth && Sample", false)]
    [InlineData("Auth || Sample", true)]
    [InlineData("!(Auth && Sample)", true)]
    [InlineData("Auth && !Sample || Tests", true)]
    public void ConditionEvaluator_EvaluatesExpressions(string expression, bool expected)
    {
        var active = new HashSet<string> { "Auth", "Tests" };

        Assert.Equal(expected, ConditionEvaluator.Evaluate(expression, active, TemplateContext.KnownSymbols));
    }
}
