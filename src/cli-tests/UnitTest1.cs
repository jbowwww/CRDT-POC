using Xunit;
using Xunit.Abstractions;

namespace cli_tests;

public class UnitTest1
{
    private readonly ITestOutputHelper output;

    public UnitTest1(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void Test1()
    {
        
    }
}