using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Tests;

public sealed class SubjectServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsTextAndAllowsExistingPrerequisite()
    {
        var repository = new FakeSubjectRepository(4);
        var service = new SubjectService(repository);

        var result = await service.CreateAsync(new CreateSubjectRequest
        {
            Name = "  Մաթեմատիկա  ",
            Description = "  Հիմնական դասընթաց  ",
            PrerequisiteSubjectId = 4
        }, CancellationToken.None);

        Assert.Equal("Մաթեմատիկա", repository.Created!.Name);
        Assert.Equal("Հիմնական դասընթաց", repository.Created.Description);
        Assert.Equal(4, result.PrerequisiteSubjectId);
    }

    [Fact]
    public async Task CreateAsync_RejectsMissingPrerequisite()
    {
        var service = new SubjectService(new FakeSubjectRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new CreateSubjectRequest { Name = "Ֆիզիկա", PrerequisiteSubjectId = 99 }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_RejectsSubjectAsItsOwnPrerequisite()
    {
        var service = new SubjectService(new FakeSubjectRepository(7));

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(7,
            new CreateSubjectRequest { Name = "Ֆիզիկա", PrerequisiteSubjectId = 7 }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_RejectsEmptyName()
    {
        var service = new SubjectService(new FakeSubjectRepository());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(
            new CreateSubjectRequest { Name = "   " }, CancellationToken.None));
    }

    private sealed class FakeSubjectRepository(params int[] existingIds) : ISubjectRepository
    {
        public CreateSubjectRequest? Created { get; private set; }

        public Task<IReadOnlyList<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SubjectResponse>>(Array.Empty<SubjectResponse>());

        public Task<bool> ExistsAsync(int subjectId, CancellationToken cancellationToken) =>
            Task.FromResult(existingIds.Contains(subjectId));

        public Task<SubjectResponse> CreateAsync(CreateSubjectRequest request, CancellationToken cancellationToken)
        {
            Created = request;
            return Task.FromResult(new SubjectResponse { Name = request.Name, PrerequisiteSubjectId = request.PrerequisiteSubjectId });
        }

        public Task<SubjectResponse?> UpdateAsync(int subjectId, CreateSubjectRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<SubjectResponse?>(new SubjectResponse { Id = subjectId, Name = request.Name });

        public Task<bool> DeleteAsync(int subjectId, CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
