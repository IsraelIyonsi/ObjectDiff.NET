using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Options;

public class CustomComparerTests
{
    private sealed class EpsilonComparer : IEqualityComparer<double>
    {
        private const double Epsilon = 0.01;

        public bool Equals(double x, double y) => Math.Abs(x - y) < Epsilon;

        public int GetHashCode(double obj) => 0;
    }

    private sealed class CityOnlyComparer : IEqualityComparer<Address>
    {
        public bool Equals(Address? x, Address? y) => x?.City == y?.City;

        public int GetHashCode(Address obj) => obj.City.GetHashCode();
    }

    [Fact]
    public void Custom_comparer_overrides_default_value_type_equality_to_treat_near_values_as_equal()
    {
        var options = new DiffOptions().UseComparer(new EpsilonComparer());

        var result = ObjectDiffer.Compare(1.000, 1.001, options);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void Custom_comparer_still_reports_a_difference_outside_its_tolerance()
    {
        var options = new DiffOptions().UseComparer(new EpsilonComparer());

        var result = ObjectDiffer.Compare(1.0, 2.0, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal(ChangeKind.Modified, change.Kind);
    }

    [Fact]
    public void Custom_comparer_on_a_reference_type_replaces_member_by_member_recursion()
    {
        var left = new Address { City = "Lagos", Country = "Nigeria" };
        var right = new Address { City = "Lagos", Country = "Federal Republic of Nigeria" };
        var options = new DiffOptions().UseComparer(new CityOnlyComparer());

        var result = ObjectDiffer.Compare(left, right, options);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void Custom_comparer_reports_the_whole_value_as_modified_without_nested_paths()
    {
        var left = new Address { City = "Lagos" };
        var right = new Address { City = "Abuja" };
        var options = new DiffOptions().UseComparer(new CityOnlyComparer());

        var result = ObjectDiffer.Compare(left, right, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal(string.Empty, change.Path);
        Assert.Same(left, change.OldValue);
        Assert.Same(right, change.NewValue);
    }

    [Fact]
    public void Use_comparer_throws_for_null_comparer()
    {
        var options = new DiffOptions();

        Assert.Throws<ArgumentNullException>(() => options.UseComparer<double>(null!));
    }
}
