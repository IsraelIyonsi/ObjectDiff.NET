using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Options;

public class MaxDepthTests
{
    private static DeepA BuildChain(string aLabel, string bLabel, string cLabel) => new()
    {
        Label = aLabel,
        B = new DeepB
        {
            Label = bLabel,
            C = new DeepC { Label = cLabel },
        },
    };

    [Fact]
    public void Default_max_depth_reaches_a_difference_three_levels_deep()
    {
        var left = BuildChain("a", "b", "x");
        var right = BuildChain("a", "b", "y");

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("B.C.Label", change.Path);
    }

    [Fact]
    public void Max_depth_of_three_still_reaches_the_third_level_difference()
    {
        var left = BuildChain("a", "b", "x");
        var right = BuildChain("a", "b", "y");
        var options = new DiffOptions { MaxDepth = 3 };

        var result = ObjectDiffer.Compare(left, right, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal("B.C.Label", change.Path);
    }

    [Fact]
    public void Max_depth_of_two_truncates_before_the_third_level_and_hides_the_difference()
    {
        var left = BuildChain("a", "b", "x");
        var right = BuildChain("a", "b", "y");
        var options = new DiffOptions { MaxDepth = 2 };

        var result = ObjectDiffer.Compare(left, right, options);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void Max_depth_of_one_only_reports_the_first_level_difference()
    {
        var left = BuildChain("left-a", "left-b", "same");
        var right = BuildChain("right-a", "right-b", "same");
        var options = new DiffOptions { MaxDepth = 1 };

        var result = ObjectDiffer.Compare(left, right, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Label", change.Path);
    }

    [Fact]
    public void Without_the_max_depth_restriction_both_first_and_second_level_differences_are_reported()
    {
        var left = BuildChain("left-a", "left-b", "same");
        var right = BuildChain("right-a", "right-b", "same");

        var result = ObjectDiffer.Compare(left, right);

        Assert.Equal(2, result.Changes.Count);
        Assert.Contains(result.Changes, c => c.Path == "Label");
        Assert.Contains(result.Changes, c => c.Path == "B.Label");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Setting_max_depth_below_the_minimum_throws_instead_of_silently_hiding_every_difference(int invalidDepth)
    {
        var options = new DiffOptions();

        Assert.Throws<ArgumentOutOfRangeException>(() => options.MaxDepth = invalidDepth);
    }

    [Fact]
    public void Max_depth_default_matches_the_documented_constant()
    {
        var options = new DiffOptions();

        Assert.Equal(DiffOptions.DefaultMaxDepth, options.MaxDepth);
    }
}
