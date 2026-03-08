using ReleasePilot.Api.Domain.Primitives;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Domain.Tests;

public class WorkItemReferenceTests
{
    [Fact]
    public void Constructor_WithBlankExternalId_ThrowsDomainRuleViolation()
    {
        var action = () => new WorkItemReference("   ", "Some title");

        var exception = Assert.Throws<DomainRuleViolationException>(action);
        Assert.Equal("Work item external id is required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithValidData_TrimsExternalIdAndTitle()
    {
        var reference = new WorkItemReference(" WI-123 ", "  Fix payment timeout  ");

        Assert.Equal("WI-123", reference.ExternalId);
        Assert.Equal("Fix payment timeout", reference.Title);
    }

    [Fact]
    public void Constructor_WithBlankTitle_SetsTitleToNull()
    {
        var reference = new WorkItemReference("WI-123", "   ");

        Assert.Equal("WI-123", reference.ExternalId);
        Assert.Null(reference.Title);
    }
}
