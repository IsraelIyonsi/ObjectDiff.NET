using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Core;

public class CycleDetectionTests
{
    [Fact]
    public void Self_referencing_equal_graphs_complete_and_report_no_changes()
    {
        var left = new Node { Name = "root" };
        left.Next = left;
        var right = new Node { Name = "root" };
        right.Next = right;

        var result = ObjectDiffer.Compare(left, right);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void Self_referencing_graphs_still_report_real_differences()
    {
        var left = new Node { Name = "left" };
        left.Next = left;
        var right = new Node { Name = "right" };
        right.Next = right;

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Name", change.Path);
    }

    [Fact]
    public void Mutual_two_node_cycle_completes_without_hanging()
    {
        var leftA = new Node { Name = "a" };
        var leftB = new Node { Name = "b" };
        leftA.Next = leftB;
        leftB.Next = leftA;

        var rightA = new Node { Name = "a" };
        var rightB = new Node { Name = "b" };
        rightA.Next = rightB;
        rightB.Next = rightA;

        var result = ObjectDiffer.Compare(leftA, rightA);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void Mutual_two_node_cycle_still_detects_a_difference()
    {
        var leftA = new Node { Name = "a" };
        var leftB = new Node { Name = "b" };
        leftA.Next = leftB;
        leftB.Next = leftA;

        var rightA = new Node { Name = "a" };
        var rightB = new Node { Name = "changed" };
        rightA.Next = rightB;
        rightB.Next = rightA;

        var result = ObjectDiffer.Compare(leftA, rightA);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Next.Name", change.Path);
    }

    [Fact]
    public void Same_object_reused_in_two_branches_without_being_cyclic_is_still_compared_correctly()
    {
        var shared = new Node { Name = "shared" };
        var left = new Node { Name = "root", Next = shared };
        var right = new Node { Name = "root", Next = new Node { Name = "shared" } };

        var result = ObjectDiffer.Compare(left, right);

        Assert.True(result.AreEqual);
    }
}
