using Publisher.Core.Models;
using Publisher.Core.Utilities;

namespace Publisher.Tests;

public class TitleResolverTests
{
    private static GlobalState BuildState() => new GlobalState();

    private static void RegisterPost(GlobalState state, string title, string slug, string date)
    {
        state.Index[slug] = new Post { Title = title, Slug = slug, Date = date };
        state.TitleToSlug[title] = slug;

        if (!state.TitleToSlugs.TryGetValue(title, out var slugs))
        {
            slugs = new List<string>();
            state.TitleToSlugs[title] = slugs;
        }
        slugs.Add(slug);
    }

    [Fact]
    public void SingleMatch_ReturnsSlug_NoWarning()
    {
        var state = BuildState();
        RegisterPost(state, "Os Suspeitos", "_os_suspeitos", "2014-10-04");

        var slug = TitleResolver.Resolve(state, "Os Suspeitos", out var warning);

        Assert.Equal("_os_suspeitos", slug);
        Assert.Null(warning);
    }

    [Fact]
    public void NoMatch_ReturnsNull_NoWarning()
    {
        var state = BuildState();

        var slug = TitleResolver.Resolve(state, "Unknown Title", out var warning);

        Assert.Null(slug);
        Assert.Null(warning);
    }

    [Fact]
    public void Ambiguous_WithMatchingLinkDate_ResolvesToThatPost_NoWarning()
    {
        var state = BuildState();
        RegisterPost(state, "Os Suspeitos", "_os_suspeitos", "2013-10-25");
        RegisterPost(state, "Os Suspeitos", "_os_suspeitos_2014_10_04", "2014-10-04");
        state.LinkDates["Os Suspeitos"] = "2013-10-25";

        var slug = TitleResolver.Resolve(state, "Os Suspeitos", out var warning);

        Assert.Equal("_os_suspeitos", slug);
        Assert.Null(warning);
    }

    [Fact]
    public void Ambiguous_WithLinkDateMatchingLaterPost_ResolvesToThatPost_NoWarning()
    {
        var state = BuildState();
        RegisterPost(state, "Os Suspeitos", "_os_suspeitos", "2013-10-25");
        RegisterPost(state, "Os Suspeitos", "_os_suspeitos_2014_10_04", "2014-10-04");
        state.LinkDates["Os Suspeitos"] = "2014-10-04";

        var slug = TitleResolver.Resolve(state, "Os Suspeitos", out var warning);

        Assert.Equal("_os_suspeitos_2014_10_04", slug);
        Assert.Null(warning);
    }

    [Fact]
    public void Ambiguous_WithNonMatchingLinkDate_WarnsAndFallsBackToLastRegistered()
    {
        var state = BuildState();
        RegisterPost(state, "Os Suspeitos", "_os_suspeitos", "2013-10-25");
        RegisterPost(state, "Os Suspeitos", "_os_suspeitos_2014_10_04", "2014-10-04");
        state.LinkDates["Os Suspeitos"] = "1999-01-01";

        var slug = TitleResolver.Resolve(state, "Os Suspeitos", out var warning);

        Assert.Equal("_os_suspeitos_2014_10_04", slug);
        Assert.NotNull(warning);
        Assert.Contains("Os Suspeitos", warning);
        Assert.Contains("1999-01-01", warning);
    }

    [Fact]
    public void Ambiguous_WithNoLinkDate_WarnsAndFallsBackToLastRegistered()
    {
        var state = BuildState();
        RegisterPost(state, "Os Suspeitos", "_os_suspeitos", "2013-10-25");
        RegisterPost(state, "Os Suspeitos", "_os_suspeitos_2014_10_04", "2014-10-04");

        var slug = TitleResolver.Resolve(state, "Os Suspeitos", out var warning);

        Assert.Equal("_os_suspeitos_2014_10_04", slug);
        Assert.NotNull(warning);
        Assert.Contains("Os Suspeitos", warning);
        Assert.Contains("no disambiguating", warning);
    }
}
