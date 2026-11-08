using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Models;
using StudentMonitoring.Server.Repositories;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Tests;

public sealed class DepartmentServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsValuesBeforeSaving()
    {
        var repository = new FakeDepartmentRepository();
        var service = new DepartmentService(repository);

        await service.CreateAsync(new CreateDepartmentRequest
        {
            Name = "  Տեղեկատվական տեխնոլոգիաներ  ",
            Description = "  Ծրագրավորում  ",
            Type = "academic"
        }, CancellationToken.None);

        Assert.Equal("Տեղեկատվական տեխնոլոգիաներ", repository.Created!.Name);
        Assert.Equal("Ծրագրավորում", repository.Created.Description);
    }

    [Fact]
    public async Task CreateAsync_RejectsEmptyName()
    {
        var service = new DepartmentService(new FakeDepartmentRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new CreateDepartmentRequest { Name = "  " }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownType()
    {
        var service = new DepartmentService(new FakeDepartmentRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new CreateDepartmentRequest { Name = "Բաժին", Type = "unknown" }, CancellationToken.None));
    }

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        public Department? Created { get; private set; }

        public Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Department>>(Array.Empty<Department>());

        public Task<Department> CreateAsync(Department department, CancellationToken cancellationToken)
        {
            Created = department;
            return Task.FromResult(department);
        }
    }
}
