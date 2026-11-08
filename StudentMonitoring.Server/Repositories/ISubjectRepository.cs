using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public interface ISubjectRepository
{
    Task<IReadOnlyList<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken);

    Task<bool> ExistsAsync(int subjectId, CancellationToken cancellationToken);

    Task<SubjectResponse> CreateAsync(CreateSubjectRequest request, CancellationToken cancellationToken);

    Task<SubjectResponse?> UpdateAsync(int subjectId, CreateSubjectRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int subjectId, CancellationToken cancellationToken);
}
