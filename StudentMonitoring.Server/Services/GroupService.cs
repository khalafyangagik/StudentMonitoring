using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class GroupService : IGroupService
{
    private readonly IGroupRepository _repository;

    public GroupService(IGroupRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<GroupResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public Task<IReadOnlyList<GroupResponse>> GetForLecturerAsync(int lecturerId, CancellationToken cancellationToken)
    {
        return _repository.GetForLecturerAsync(lecturerId, cancellationToken);
    }

    public async Task<GroupResponse> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken)
    {
        return await _repository.CreateAsync(await ValidateAndNormalizeAsync(request, cancellationToken), cancellationToken);
    }

    public async Task<GroupResponse?> UpdateAsync(int groupId, CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var normalized = await ValidateAndNormalizeAsync(request, cancellationToken);
        return await _repository.UpdateAsync(groupId, normalized, cancellationToken);
    }

    public Task<bool> DeleteAsync(int groupId, CancellationToken cancellationToken)
    {
        return _repository.DeleteAsync(groupId, cancellationToken);
    }

    private async Task<CreateGroupRequest> ValidateAndNormalizeAsync(CreateGroupRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Group name is required.", nameof(request));
        }

        if (request.StudentCount is < 0)
        {
            throw new ArgumentException("Student count cannot be negative.", nameof(request));
        }

        if (request.CourseId is int courseId && !await _repository.CourseExistsAsync(courseId, cancellationToken))
        {
            throw new ArgumentException("The selected course does not exist.", nameof(request));
        }

        return new CreateGroupRequest
        {
            Name = name,
            CourseId = request.CourseId,
            AcademicYear = request.AcademicYear?.Trim(),
            StudentCount = request.StudentCount ?? 0
        };
    }
}
