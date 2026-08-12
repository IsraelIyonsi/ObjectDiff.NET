using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Core;

public class NestedObjectTests
{
    [Fact]
    public void Equal_objects_produce_no_changes()
    {
        var left = new Person { Name = "Ada", Age = 30, Address = new Address { City = "Lagos", Country = "Nigeria" } };
        var right = new Person { Name = "Ada", Age = 30, Address = new Address { City = "Lagos", Country = "Nigeria" } };

        var result = ObjectDiffer.Compare(left, right);

        Assert.True(result.AreEqual);
        Assert.Empty(result.Changes);
    }

    [Fact]
    public void Scalar_member_change_is_reported_as_modified_with_member_path()
    {
        var left = new Person { Name = "Ada", Age = 30 };
        var right = new Person { Name = "Grace", Age = 30 };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Name", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal("Ada", change.OldValue);
        Assert.Equal("Grace", change.NewValue);
    }

    [Fact]
    public void Nested_object_member_change_uses_dotted_path()
    {
        var left = new Person { Name = "Ada", Address = new Address { City = "Lagos", Country = "Nigeria" } };
        var right = new Person { Name = "Ada", Address = new Address { City = "Abuja", Country = "Nigeria" } };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Address.City", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal("Lagos", change.OldValue);
        Assert.Equal("Abuja", change.NewValue);
    }

    [Fact]
    public void Nested_member_becoming_null_is_modified_not_removed()
    {
        var left = new Person { Name = "Ada", Address = new Address { City = "Lagos" } };
        var right = new Person { Name = "Ada", Address = null };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Address", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.NotNull(change.OldValue);
        Assert.Null(change.NewValue);
    }

    [Fact]
    public void Nested_member_appearing_from_null_is_modified_not_added()
    {
        var left = new Person { Name = "Ada", Address = null };
        var right = new Person { Name = "Ada", Address = new Address { City = "Lagos" } };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Address", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Null(change.OldValue);
        Assert.NotNull(change.NewValue);
    }

    [Fact]
    public void Both_null_top_level_objects_are_equal()
    {
        var result = ObjectDiffer.Compare<Person>(null, null);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void One_null_top_level_object_is_a_single_root_modification()
    {
        var right = new Person { Name = "Ada" };

        var result = ObjectDiffer.Compare<Person>(null, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal(string.Empty, change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Null(change.OldValue);
        Assert.Same(right, change.NewValue);
    }
}
