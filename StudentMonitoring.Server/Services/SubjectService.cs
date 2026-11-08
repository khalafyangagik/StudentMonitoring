using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class SubjectService : ISubjectService
{
    private readonly ISubjectRepository _repository;

    public SubjectService(ISubjectRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _repository.GetAllAsync(cancellationToken);
    }

    public async Task<SubjectResponse> CreateAsync(CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        return await _repository.CreateAsync(await ValidateAndNormalizeAsync(request, null, cancellationToken), cancellationToken);
    }

    public async Task<SubjectResponse?> UpdateAsync(int subjectId, CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        if (!await _repository.ExistsAsync(subjectId, cancellationToken))
        {
            return null;
        }

        var normalized = await ValidateAndNormalizeAsync(request, subjectId, cancellationToken);
        return await _repository.UpdateAsync(subjectId, normalized, cancellationToken);
    }

    public Task<bool> DeleteAsync(int subjectId, CancellationToken cancellationToken)
    {
        return _repository.DeleteAsync(subjectId, cancellationToken);
    }

    private async Task<CreateSubjectRequest> ValidateAndNormalizeAsync(
        CreateSubjectRequest request,
        int? subjectId,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            throw new ArgumentException("Subject name is required.", nameof(request));
        }

        if (request.PrerequisiteSubjectId is int prerequisiteId)
        {
            if (subjectId == prerequisiteId)
            {
                throw new ArgumentException("A subject cannot be its own prerequisite.", nameof(request));
            }

            if (!await _repository.ExistsAsync(prerequisiteId, cancellationToken))
            {
                throw new ArgumentException("The selected prerequisite subject does not exist.", nameof(request));
            }
        }

        return new CreateSubjectRequest
        {
            Name = name,
            Description = request.Description?.Trim(),
            PrerequisiteSubjectId = request.PrerequisiteSubjectId
        };
    }
}
