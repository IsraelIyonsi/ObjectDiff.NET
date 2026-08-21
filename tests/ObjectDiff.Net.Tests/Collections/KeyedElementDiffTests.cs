using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Collections;

public class KeyedElementDiffTests
{
    [Fact]
    public void Match_collection_elements_by_key_returns_the_same_options_instance()
    {
        var options = new DiffOptions();

        var returned = options.MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref);

        Assert.Same(options, returned);
    }

    [Fact]
    public void Match_collection_elements_by_key_throws_for_a_null_selector()
    {
        var options = new DiffOptions();

        Assert.Throws<ArgumentNullException>(() => options.MatchCollectionElementsByKey<KeyedOrder>(null!));
    }

    [Fact]
    public void Reordered_list_produces_no_changes_with_a_selector_but_positional_noise_without_one()
    {
        var left = new List<KeyedOrder>
        {
            new() { Ref = "A", Total = 1m },
            new() { Ref = "B", Total = 2m },
        };
        var right = new List<KeyedOrder>
        {
            new() { Ref = "B", Total = 2m },
            new() { Ref = "A", Total = 1m },
        };

        var keyed = ObjectDiffer.Compare(left, right, new DiffOptions().MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref));
        var positional = ObjectDiffer.Compare(left, right);

        Assert.True(keyed.AreEqual);
        Assert.False(positional.AreEqual);
    }

    [Fact]
    public void A_key_present_only_on_the_right_is_a_single_added_at_the_keyed_path()
    {
        var left = new List<KeyedOrder> { new() { Ref = "A", Total = 1m } };
        var right = new List<KeyedOrder>
        {
            new() { Ref = "A", Total = 1m },
            new() { Ref = "B", Total = 5m },
        };
        var options = new DiffOptions().MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref);

        var result = ObjectDiffer.Compare(left, right, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[\"B\"]", change.Path);
        Assert.Equal(ChangeKind.Added, change.Kind);
        Assert.Equal("B", Assert.IsType<KeyedOrder>(change.NewValue).Ref);
    }

    [Fact]
    public void A_key_present_only_on_the_left_is_a_single_removed_at_the_keyed_path()
    {
        var left = new List<KeyedOrder>
        {
            new() { Ref = "A", Total = 1m },
            new() { Ref = "B", Total = 5m },
        };
        var right = new List<KeyedOrder> { new() { Ref = "A", Total = 1m } };
        var options = new DiffOptions().MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref);

        var result = ObjectDiffer.Compare(left, right, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[\"B\"]", change.Path);
        Assert.Equal(ChangeKind.Removed, change.Kind);
        Assert.Equal("B", Assert.IsType<KeyedOrder>(change.OldValue).Ref);
    }

    [Fact]
    public void A_changed_element_recurses_into_a_nested_change_at_the_stable_keyed_path()
    {
        var left = new OrderBook { Orders = { new() { Ref = "ORD-9", Total = 10m } } };
        var right = new OrderBook { Orders = { new() { Ref = "ORD-9", Total = 20m } } };
        var options = new DiffOptions().MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref);

        var result = ObjectDiffer.Compare(left, right, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal("Orders[\"ORD-9\"].Total", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal(10m, change.OldValue);
        Assert.Equal(20m, change.NewValue);
    }

    [Fact]
    public void A_keyed_list_nested_inside_a_keyed_element_matches_by_key_at_both_levels()
    {
        var left = new List<KeyedOrder>
        {
            new()
            {
                Ref = "ORD-9",
                Lines =
                {
                    new() { Sku = "S1", Quantity = 1 },
                    new() { Sku = "S2", Quantity = 2 },
                },
            },
        };
        var right = new List<KeyedOrder>
        {
            new()
            {
                Ref = "ORD-9",
                Lines =
                {
                    new() { Sku = "S2", Quantity = 2 },
                    new() { Sku = "S1", Quantity = 9 },
                },
            },
        };
        var options = new DiffOptions()
            .MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref)
            .MatchCollectionElementsByKey<KeyedLine>(l => l.Sku);

        var result = ObjectDiffer.Compare(left, right, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[\"ORD-9\"].Lines[\"S1\"].Quantity", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal(1, change.OldValue);
        Assert.Equal(9, change.NewValue);
    }

    [Fact]
    public void A_duplicate_key_on_one_side_aborts_the_comparison_with_a_clear_exception()
    {
        var left = new List<KeyedOrder>
        {
            new() { Ref = "X", Total = 1m },
            new() { Ref = "X", Total = 2m },
        };
        var right = new List<KeyedOrder>();
        var options = new DiffOptions().MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref);

        var exception = Assert.Throws<ObjectDiffException>(() => ObjectDiffer.Compare(left, right, options));

        Assert.Contains("more than one element", exception.Message, StringComparison.Ordinal);
        Assert.Contains("X", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Null_elements_are_matched_by_a_single_null_key_slot_and_ignore_order()
    {
        var left = new List<KeyedOrder?> { new() { Ref = "A", Total = 1m }, null };
        var right = new List<KeyedOrder?> { null, new() { Ref = "A", Total = 1m } };
        var options = new DiffOptions().MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref);

        var result = ObjectDiffer.Compare(left, right, options);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void An_element_with_a_null_key_recurses_under_the_null_slot_path()
    {
        var left = new List<KeyedOrder> { new() { Ref = null, Total = 1m } };
        var right = new List<KeyedOrder> { new() { Ref = null, Total = 2m } };
        var options = new DiffOptions().MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref);

        var result = ObjectDiffer.Compare(left, right, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal("[null].Total", change.Path);
        Assert.Equal(ChangeKind.Modified, change.Kind);
        Assert.Equal(1m, change.OldValue);
        Assert.Equal(2m, change.NewValue);
    }

    [Fact]
    public void A_null_element_and_a_null_key_element_on_the_same_side_collide_as_a_duplicate()
    {
        var left = new List<KeyedOrder?> { null, new() { Ref = null, Total = 1m } };
        var right = new List<KeyedOrder?>();
        var options = new DiffOptions().MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref);

        Assert.Throws<ObjectDiffException>(() => ObjectDiffer.Compare(left, right, options));
    }

    [Fact]
    public void A_collection_whose_element_type_has_no_selector_falls_back_to_positional_alongside_a_keyed_one()
    {
        var left = new OrderBook
        {
            Orders = { new() { Ref = "A", Total = 1m }, new() { Ref = "B", Total = 2m } },
            Tags = { "x", "y" },
        };
        var right = new OrderBook
        {
            Orders = { new() { Ref = "B", Total = 2m }, new() { Ref = "A", Total = 1m } },
            Tags = { "y", "x" },
        };
        var options = new DiffOptions().MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref);

        var result = ObjectDiffer.Compare(left, right, options);

        Assert.False(result.AreEqual);
        Assert.All(result.Changes, c => Assert.StartsWith("Tags", c.Path, StringComparison.Ordinal));
        Assert.DoesNotContain(result.Changes, c => c.Path.StartsWith("Orders", StringComparison.Ordinal));
    }

    [Fact]
    public void Ignore_rules_are_respected_inside_a_recursed_keyed_element()
    {
        var left = new List<KeyedOrder> { new() { Ref = "A", Total = 1m } };
        var right = new List<KeyedOrder> { new() { Ref = "A", Total = 999m } };
        var options = new DiffOptions()
            .MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref)
            .IgnoreMember<KeyedOrder>(nameof(KeyedOrder.Total));

        var result = ObjectDiffer.Compare(left, right, options);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void A_custom_comparer_is_respected_inside_a_recursed_keyed_element()
    {
        var left = new List<KeyedOrder> { new() { Ref = "A", Total = 1.000m } };
        var right = new List<KeyedOrder> { new() { Ref = "A", Total = 1.001m } };
        var options = new DiffOptions()
            .MatchCollectionElementsByKey<KeyedOrder>(o => o.Ref)
            .UseComparer(new NearDecimalComparer());

        var result = ObjectDiffer.Compare(left, right, options);

        Assert.True(result.AreEqual);
    }

    private sealed class NearDecimalComparer : IEqualityComparer<decimal>
    {
        private const decimal Tolerance = 0.01m;

        public bool Equals(decimal x, decimal y) => Math.Abs(x - y) < Tolerance;

        public int GetHashCode(decimal obj) => 0;
    }
}
