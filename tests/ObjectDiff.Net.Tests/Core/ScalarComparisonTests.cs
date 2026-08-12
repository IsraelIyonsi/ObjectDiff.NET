using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Core;

public class ScalarComparisonTests
{
    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    [InlineData(-5, -5, true)]
    [InlineData(int.MinValue, int.MaxValue, false)]
    public void Ints_compare_directly(int left, int right, bool expectedEqual)
    {
        var result = ObjectDiffer.Compare(left, right);
        Assert.Equal(expectedEqual, result.AreEqual);
    }

    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("a", "b", false)]
    [InlineData("", "", true)]
    [InlineData(null, "a", false)]
    [InlineData("a", null, false)]
    [InlineData(null, null, true)]
    public void Strings_compare_directly(string? left, string? right, bool expectedEqual)
    {
        var result = ObjectDiffer.Compare(left, right);
        Assert.Equal(expectedEqual, result.AreEqual);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    public void Bools_compare_directly(bool left, bool right, bool expectedEqual)
    {
        var result = ObjectDiffer.Compare(left, right);
        Assert.Equal(expectedEqual, result.AreEqual);
    }

    [Theory]
    [InlineData(Status.Draft, Status.Draft, true)]
    [InlineData(Status.Draft, Status.Active, false)]
    public void Enums_compare_directly(Status left, Status right, bool expectedEqual)
    {
        var result = ObjectDiffer.Compare(left, right);
        Assert.Equal(expectedEqual, result.AreEqual);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    [InlineData(null, 1, false)]
    [InlineData(1, null, false)]
    public void Nullable_value_types_compare_directly(int? left, int? right, bool expectedEqual)
    {
        var result = ObjectDiffer.Compare(left, right);
        Assert.Equal(expectedEqual, result.AreEqual);
    }

    public static IEnumerable<object?[]> NonConstantScalarCases()
    {
        yield return new object?[] { 1.5m, 1.5m, true };
        yield return new object?[] { 1.5m, 1.6m, false };
        yield return new object?[] { new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), true };
        yield return new object?[] { new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), false };
        yield return new object?[] { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("11111111-1111-1111-1111-111111111111"), true };
        yield return new object?[] { Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), false };
        yield return new object?[] { double.NaN, double.NaN, true };
        yield return new object?[] { 1.0, 2.0, false };
    }

    [Theory]
    [MemberData(nameof(NonConstantScalarCases))]
    public void Bcl_value_types_compare_directly(object left, object right, bool expectedEqual)
    {
        var result = CompareBoxed(left, right);
        Assert.Equal(expectedEqual, result.AreEqual);
    }

    [Theory]
    [InlineData(1, 1, 1, 1, true)]
    [InlineData(1, 1, 1, 2, false)]
    [InlineData(1, 2, 2, 2, false)]
    public void Structs_without_overridden_equals_compare_by_default_field_equality(
        int leftX, int leftY, int rightX, int rightY, bool expectedEqual)
    {
        var left = new Point(leftX, leftY);
        var right = new Point(rightX, rightY);

        var result = ObjectDiffer.Compare(left, right);

        Assert.Equal(expectedEqual, result.AreEqual);
    }

    [Fact]
    public void Root_level_scalar_difference_uses_empty_path()
    {
        var result = ObjectDiffer.Compare(1, 2);

        var change = Assert.Single(result.Changes);
        Assert.Equal(string.Empty, change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal(1, change.OldValue);
        Assert.Equal(2, change.NewValue);
    }

    [Fact]
    public void Identical_reference_short_circuits_as_equal()
    {
        var shared = new Person { Name = "Ada" };

        var result = ObjectDiffer.Compare(shared, shared);

        Assert.True(result.AreEqual);
        Assert.Empty(result.Changes);
    }

    [Fact]
    public void Different_runtime_types_behind_a_common_base_are_reported_as_modified()
    {
        Shape left = new Circle { Radius = 3 };
        Shape right = new Square { Side = 3 };

        var result = ObjectDiffer.Compare(left, right);

        var change = Assert.Single(result.Changes);
        Assert.Equal(string.Empty, change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.IsType<Circle>(change.OldValue);
        Assert.IsType<Square>(change.NewValue);
    }

    private static DiffResult CompareBoxed(object left, object right)
    {
        var method = typeof(ObjectDiffer)
            .GetMethods()
            .First(m => m.Name == nameof(ObjectDiffer.Compare) && m.GetParameters().Length == 2);
        var generic = method.MakeGenericMethod(left.GetType());
        return (DiffResult)generic.Invoke(null, new[] { left, right })!;
    }
}
