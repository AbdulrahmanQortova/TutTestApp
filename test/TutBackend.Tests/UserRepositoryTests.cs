using Xunit;
using Microsoft.EntityFrameworkCore;
using TutBackend.Data;
using TutBackend.Repositories;
using Tut.Common.Models;

namespace TutBackend.Tests;

public class UserRepositoryTests
{
    private TutDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TutDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new TutDbContext(options);
    }

    [Fact]
    public async Task GetByMobileAsync_WithExistingUser_ReturnsUser()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        var user = new User { Mobile = "1234567890", FirstName = "John", LastName = "Doe" };
        await repository.AddAsync(user);

        // Act
        var result = await repository.GetByMobileAsync("1234567890");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("1234567890", result.Mobile);
    }

    [Fact]
    public async Task GetByMobileAsync_WithNonExistingUser_ReturnsNull()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByMobileAsync("9999999999");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_AddsUserToDatabase()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        var user = new User { Mobile = "5555555555", FirstName = "Jane", LastName = "Smith" };

        // Act
        var addedUser = await repository.AddAsync(user);

        // Assert
        Assert.NotNull(addedUser);
        Assert.True(addedUser.Id > 0);
        var retrieved = await repository.GetByIdAsync(addedUser.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Jane", retrieved.FirstName);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUserInDatabase()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        var user = new User { Mobile = "1111111111", FirstName = "Old", LastName = "Name" };
        await repository.AddAsync(user);

        // Act
        user.FirstName = "New";
        await repository.UpdateAsync(user);

        // Assert
        var updated = await repository.GetByIdAsync(user.Id);
        Assert.NotNull(updated);
        Assert.Equal("New", updated.FirstName);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUserFromDatabase()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        var user = new User { Mobile = "2222222222", FirstName = "Delete", LastName = "Me" };
        await repository.AddAsync(user);
        var userId = user.Id;

        // Act
        await repository.DeleteAsync(userId);

        // Assert
        var deleted = await repository.GetByIdAsync(userId);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var repository = new UserRepository(context);
        await repository.AddAsync(new User { Mobile = "1111111111", FirstName = "User1", LastName = "Test" });
        await repository.AddAsync(new User { Mobile = "2222222222", FirstName = "User2", LastName = "Test" });
        await repository.AddAsync(new User { Mobile = "3333333333", FirstName = "User3", LastName = "Test" });

        // Act
        var users = await repository.GetAllAsync();

        // Assert
        var userList = users.ToList();
        Assert.Equal(3, userList.Count);
    }
}
