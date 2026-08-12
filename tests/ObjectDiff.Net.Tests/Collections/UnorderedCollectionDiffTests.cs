using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Collections;

public class UnorderedCollectionDiffTests
{
    [Fact]
    public void Sets_with_the_same_members_in_a_different_enumeration_order_produce_no_changes()
    {
        var left = new HashSet<string> { "vip", "beta", "legacy" };
        var right = new HashSet<string> { "legacy", "vip", "beta" };

        var result = ObjectDiffer.Compare(left, right);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void A_member_only_in_the_left_set_is_removed()
    {
        var left = new HashSet<string> { "vip", "beta" };
        var right = new HashSet<string> { "vip" };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal(ChangeKind.Removed, change.Kind);
        Assert.Equal("beta", change.OldValue);
    }

    [Fact]
    public void A_member_only_in_the_right_set_is_added()
    {
        var left = new HashSet<string> { "vip" };
        var right = new HashSet<string> { "vip", "beta" };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal(ChangeKind.Added, change.Kind);
        Assert.Equal("beta", change.NewValue);
    }

    [Fact]
    public void Reordered_sets_of_complex_elements_still_produce_no_changes()
    {
        var left = new HashSet<Order>(new OrderStructuralComparer())
        {
            new() { Id = 1, Total = 10m },
            new() { Id = 2, Total = 20m },
        };
        var right = new HashSet<Order>(new OrderStructuralComparer())
        {
            new() { Id = 2, Total = 20m },
            new() { Id = 1, Total = 10m },
        };

        var result = ObjectDiffer.Compare(left, right);

        Assert.True(result.AreEqual);
    }

    private sealed class OrderStructuralComparer : IEqualityComparer<Order>
    {
        public bool Equals(Order? x, Order? y) => x?.Id == y?.Id && x?.Total == y?.Total;

        public int GetHashCode(Order obj) => obj.Id.GetHashCode();
    }
}
