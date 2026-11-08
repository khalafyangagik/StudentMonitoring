using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Services;

public interface ISubjectService
{
    Task<IReadOnlyList<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<SubjectResponse> CreateAsync(CreateSubjectRequest request, CancellationToken cancellationToken);

    Task<SubjectResponse?> UpdateAsync(int subjectId, CreateSubjectRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int subjectId, CancellationToken cancellationToken);
}
