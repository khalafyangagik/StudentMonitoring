using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class LecturerPortalService(ILecturerPortalRepository repository, ILecturerGradeRepository gradeRepository)
{
    public Task<LecturerPortalSummary?> GetSummaryAsync(int lecturerId, CancellationToken cancellationToken) =>
        repository.GetSummaryAsync(lecturerId, cancellationToken);

    public Task<IReadOnlyList<LecturerStudentOption>?> GetStudentsAsync(int lecturerId, int offeringId,
        CancellationToken cancellationToken) => gradeRepository.GetStudentsAsync(lecturerId, offeringId, cancellationToken);

    public Task<IReadOnlyList<LecturerSessionOption>?> GetSessionsAsync(int lecturerId, int offeringId,
        CancellationToken cancellationToken) => gradeRepository.GetSessionsAsync(lecturerId, offeringId, cancellationToken);

    public Task<LecturerGradeCreated?> CreateGradeAsync(int lecturerId, CreateLecturerGradeRequest request,
        CancellationToken cancellationToken) => gradeRepository.CreateAsync(lecturerId, request, cancellationToken);
}
