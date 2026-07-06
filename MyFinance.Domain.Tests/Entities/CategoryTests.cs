using MyFinance.Domain.Entities;

namespace MyFinance.Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    public void Constructor_WithValidData_SetsNameAndUserId()
    {
        var userId = Guid.NewGuid();

        var category = new Category("Alimentação", userId);

        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.Equal("Alimentação", category.Name);
        Assert.Equal(userId, category.UserId);
    }

    [Fact]
    public void Constructor_GeneratesDifferentIdsForEachCategory()
    {
        var userId = Guid.NewGuid();

        var first = new Category("Categoria A", userId);
        var second = new Category("Categoria B", userId);

        Assert.NotEqual(first.Id, second.Id);
    }
}
