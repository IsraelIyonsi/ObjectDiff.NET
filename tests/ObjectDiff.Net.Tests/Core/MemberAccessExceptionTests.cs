using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Core;

public class MemberAccessExceptionTests
{
    [Fact]
    public void A_throwing_property_getter_aborts_the_comparison_with_a_diagnostic_exception()
    {
        var left = new WithFaultyProperty { Name = "a" };
        var right = new WithFaultyProperty { Name = "b" };

        var exception = Assert.Throws<ObjectDiffException>(() => ObjectDiffer.Compare(left, right));

        Assert.Contains(nameof(WithFaultyProperty.Faulty), exception.Message);
        Assert.Contains(nameof(WithFaultyProperty), exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }
}
