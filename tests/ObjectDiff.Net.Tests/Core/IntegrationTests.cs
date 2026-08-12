using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Core;

public class IntegrationTests
{
    [Fact]
    public void A_container_with_list_and_dictionary_members_produces_the_exact_expected_change_set()
    {
        var left = new Container
        {
            Orders = new List<Order>
            {
                new() { Id = 1, Total = 10m },
                new() { Id = 2, Total = 20m },
            },
            Settings = new Dictionary<string, string> { ["theme"] = "dark" },
            Scores = new Dictionary<int, string> { [1] = "gold" },
        };

        var right = new Container
        {
            Orders = new List<Order>
            {
                new() { Id = 1, Total = 10m },
                new() { Id = 3, Total = 30m },
                new() { Id = 2, Total = 20m },
            },
            Settings = new Dictionary<string, string> { ["theme"] = "light", ["locale"] = "en" },
            Scores = new Dictionary<int, string>(),
        };

        var result = ObjectDiffer.Compare(left, right);

        Assert.False(result.AreEqual);

        var ordersAdd = Assert.Single(result.Changes, c => c.Path == "Orders[1]");
        Assert.Equal(ChangeKind.Added, ordersAdd.Kind);
        Assert.Null(ordersAdd.OldValue);
        Assert.IsType<Order>(ordersAdd.NewValue);
        Assert.Equal(3, ((Order)ordersAdd.NewValue!).Id);

        var settingsModified = Assert.Single(result.Changes, c => c.Path == "Settings[\"theme\"]");
        Assert.Equal(ChangeKind.Modified, settingsModified.Kind);
        Assert.Equal("dark", settingsModified.OldValue);
        Assert.Equal("light", settingsModified.NewValue);

        var settingsAdded = Assert.Single(result.Changes, c => c.Path == "Settings[\"locale\"]");
        Assert.Equal(ChangeKind.Added, settingsAdded.Kind);
        Assert.Equal("en", settingsAdded.NewValue);

        var scoreRemoved = Assert.Single(result.Changes, c => c.Path == "Scores[1]");
        Assert.Equal(ChangeKind.Removed, scoreRemoved.Kind);
        Assert.Equal("gold", scoreRemoved.OldValue);

        Assert.Equal(4, result.Changes.Count);
    }

    [Fact]
    public void The_same_container_change_set_renders_as_a_stable_human_readable_summary()
    {
        var left = new Container { Settings = new Dictionary<string, string> { ["theme"] = "dark" } };
        var right = new Container { Settings = new Dictionary<string, string> { ["theme"] = "light" } };

        var result = ObjectDiffer.Compare(left, right);
        var summary = ChangeSummaryFormatter.Format(result);

        Assert.Equal("Modified Settings[\"theme\"]: \"dark\" -> \"light\"", summary);
    }
}
