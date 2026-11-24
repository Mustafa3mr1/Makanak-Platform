using Makanak.Application.Common.Models;
using Xunit;

namespace Makanak.Tests;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_ShouldComputeTotalPagesCorrectly()
    {
        var items = new List<string> { "item1", "item2" };
        var result = new PagedResult<string>(items, totalCount: 25, page: 1, pageSize: 10);

        Assert.Equal(3, result.TotalPages);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public void PagedResult_ShouldCapPageSizeAt50()
    {
        var items = new List<string>();
        var result = new PagedResult<string>(items, totalCount: 100, page: 1, pageSize: 200);

        Assert.Equal(PagedResult<string>.MaxPageSize, result.PageSize);
        Assert.Equal(50, result.PageSize);
    }

    [Fact]
    public void PagedResult_ShouldDefaultPageSizeTo12_WhenZeroOrNegative()
    {
        var items = new List<string>();
        var result = new PagedResult<string>(items, totalCount: 10, page: 1, pageSize: 0);

        Assert.Equal(PagedResult<string>.DefaultPageSize, result.PageSize);
        Assert.Equal(12, result.PageSize);
    }
}
