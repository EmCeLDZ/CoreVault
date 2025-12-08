using FluentAssertions;
using Xunit;

namespace CoreVault.Tests;

[Trait("Category", "Unit")]
public class SimpleUnitTests
{
    [Fact]
    public void BasicMath_ShouldWork()
    {
        // To jest test sprawdzający czy środowisko testowe w ogóle działa
        int a = 5;
        int b = 10;
        int sum = a + b;

        sum.Should().Be(15);
    }
}
