using DocuSignWebhook.Domain.Entities;

namespace DocuSignWebhook.Tests.Domain;

public class BaseEntityTests
{
    private class TestEntity : BaseEntity { }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.True(entity.CreatedAt <= DateTime.UtcNow);
        Assert.True(entity.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
        Assert.Null(entity.UpdatedAt);
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Id_CanBeSet()
    {
        // Arrange
        var entity = new TestEntity();
        var newId = Guid.NewGuid();

        // Act
        entity.Id = newId;

        // Assert
        Assert.Equal(newId, entity.Id);
    }

    [Fact]
    public void CreatedAt_CanBeSet()
    {
        // Arrange
        var entity = new TestEntity();
        var testDate = DateTime.UtcNow.AddDays(-1);

        // Act
        entity.CreatedAt = testDate;

        // Assert
        Assert.Equal(testDate, entity.CreatedAt);
    }

    [Fact]
    public void UpdatedAt_CanBeSet()
    {
        // Arrange
        var entity = new TestEntity();
        var testDate = DateTime.UtcNow;

        // Act
        entity.UpdatedAt = testDate;

        // Assert
        Assert.Equal(testDate, entity.UpdatedAt);
    }

    [Fact]
    public void IsActive_CanBeToggled()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        entity.IsActive = false;

        // Assert
        Assert.False(entity.IsActive);
    }
}
