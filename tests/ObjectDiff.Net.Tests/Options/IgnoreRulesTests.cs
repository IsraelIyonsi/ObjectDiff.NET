using ObjectDiff.Tests.Fixtures;

namespace ObjectDiff.Tests.Options;

public class IgnoreRulesTests
{
    [Fact]
    public void Ignored_member_difference_is_not_reported()
    {
        var left = new WithSecret { Username = "ada", Password = "old" };
        var right = new WithSecret { Username = "ada", Password = "new" };
        var options = new DiffOptions().IgnoreMember<WithSecret>(nameof(WithSecret.Password));

        var result = ObjectDiffer.Compare(left, right, options);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void Ignoring_one_member_still_reports_other_member_differences()
    {
        var left = new WithSecret { Username = "ada", Password = "old" };
        var right = new WithSecret { Username = "grace", Password = "new" };
        var options = new DiffOptions().IgnoreMember<WithSecret>(nameof(WithSecret.Password));

        var result = ObjectDiffer.Compare(left, right, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal(nameof(WithSecret.Username), change.Path);
    }

    [Fact]
    public void Ignored_type_hides_differences_wherever_that_type_appears()
    {
        var left = new WithLogger { Name = "svc", Logger = new LoggerHandle { Id = "one" } };
        var right = new WithLogger { Name = "svc", Logger = new LoggerHandle { Id = "two" } };
        var options = new DiffOptions().IgnoreType<LoggerHandle>();

        var result = ObjectDiffer.Compare(left, right, options);

        Assert.True(result.AreEqual);
    }

    [Fact]
    public void Ignoring_a_type_still_reports_differences_on_other_members()
    {
        var left = new WithLogger { Name = "svc-old", Logger = new LoggerHandle { Id = "one" } };
        var right = new WithLogger { Name = "svc-new", Logger = new LoggerHandle { Id = "two" } };
        var options = new DiffOptions().IgnoreType<LoggerHandle>();

        var result = ObjectDiffer.Compare(left, right, options);

        var change = Assert.Single(result.Changes);
        Assert.Equal(nameof(WithLogger.Name), change.Path);
    }

    [Fact]
    public void Ignore_type_and_ignore_member_can_be_combined_via_chained_calls()
    {
        var options = new DiffOptions()
            .IgnoreType<LoggerHandle>()
            .IgnoreMember<WithSecret>("Password")
            .IgnoreMember<WithSecret>("Password");

        Assert.NotNull(options);
    }

    [Fact]
    public void Ignore_type_throws_for_null_type()
    {
        var options = new DiffOptions();

        Assert.Throws<ArgumentNullException>(() => options.IgnoreType(null!));
    }

    [Fact]
    public void Ignore_member_throws_for_null_declaring_type()
    {
        var options = new DiffOptions();

        Assert.Throws<ArgumentNullException>(() => options.IgnoreMember(null!, "Name"));
    }

    [Fact]
    public void Ignore_member_throws_argument_null_exception_for_null_member_name()
    {
        var options = new DiffOptions();

        Assert.Throws<ArgumentNullException>(() => options.IgnoreMember(typeof(WithSecret), null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ignore_member_throws_argument_exception_for_blank_member_name(string memberName)
    {
        var options = new DiffOptions();

        Assert.Throws<ArgumentException>(() => options.IgnoreMember(typeof(WithSecret), memberName));
    }
}
