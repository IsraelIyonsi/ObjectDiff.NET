using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Core;

public class PathDeterminismTests
{
    [Fact]
    public void Members_are_reported_in_alphabetical_order_regardless_of_declaration_order()
    {
        var left = new Person
        {
            Name = "Ada",
            Age = 30,
            Nickname = "old-nick",
            Tags = new List<string>(),
        };
        var right = new Person
        {
            Name = "Grace",
            Age = 31,
            Nickname = "new-nick",
            Tags = new List<string> { "added" },
        };

        var result = ObjectDiffer.Compare(left, right);

        Assert.Equal(
            new[] { "Age", "Name", "Nickname", "Tags[0]" },
            result.Changes.Select(c => c.Path));
    }

    [Fact]
    public void Repeated_comparisons_of_the_same_inputs_produce_identical_ordering()
    {
        var left = new Person { Name = "Ada", Age = 30, Nickname = "a", Tags = { "x", "y" } };
        var right = new Person { Name = "Grace", Age = 31, Nickname = "b", Tags = { "x", "z" } };

        var first = ObjectDiffer.Compare(left, right).Changes.Select(c => c.Path).ToList();
        var second = ObjectDiffer.Compare(left, right).Changes.Select(c => c.Path).ToList();

        Assert.Equal(first, second);
    }
}
