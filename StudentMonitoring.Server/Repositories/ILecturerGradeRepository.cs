using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public interface ILecturerGradeRepository
{
    Task<IReadOnlyList<LecturerStudentOption>?> GetStudentsAsync(int lecturerId, int offeringId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LecturerSessionOption>?> GetSessionsAsync(int lecturerId, int offeringId, CancellationToken cancellationToken);
    Task<LecturerGradeCreated?> CreateAsync(int lecturerId, CreateLecturerGradeRequest request, CancellationToken cancellationToken);
}
