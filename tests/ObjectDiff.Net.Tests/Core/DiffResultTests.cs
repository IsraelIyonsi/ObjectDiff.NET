using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Core;

public class DiffResultTests
{
    [Fact]
    public void Are_equal_is_true_exactly_when_there_are_no_changes()
    {
        var equalResult = ObjectDiffer.Compare(new Person { Name = "Ada" }, new Person { Name = "Ada" });
        var differentResult = ObjectDiffer.Compare(new Person { Name = "Ada" }, new Person { Name = "Grace" });

        Assert.True(equalResult.AreEqual);
        Assert.Empty(equalResult.Changes);
        Assert.False(differentResult.AreEqual);
        Assert.NotEmpty(differentResult.Changes);
    }

    [Fact]
    public void Two_argument_overload_uses_default_options()
    {
        var left = new Person { Name = "Ada" };
        var right = new Person { Name = "Ada" };

        var result = ObjectDiffer.Compare(left, right);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void Three_argument_overload_throws_for_null_options()
    {
        Assert.Throws<ArgumentNullException>(() => ObjectDiffer.Compare(1, 2, null!));
    }

    [Fact]
    public void Change_constructor_throws_for_null_path()
    {
        Assert.Throws<ArgumentNullException>(() => new Change(null!, ChangeKind.Modified, 1, 2));
    }

    [Fact]
    public void Change_exposes_the_values_passed_to_its_constructor()
    {
        var change = new Change("Name", ChangeKind.Modified, "old", "new");

        Assert.Equal("Name", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal("old", change.OldValue);
        Assert.Equal("new", change.NewValue);
    }
}
