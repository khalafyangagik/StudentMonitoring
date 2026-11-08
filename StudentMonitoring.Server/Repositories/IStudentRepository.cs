using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public interface IStudentRepository
{
    Task<IReadOnlyList<StudentResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> GroupExistsAsync(int groupId, CancellationToken cancellationToken);
    Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken);
    Task<StudentResponse?> UpdateAsync(int studentId, CreateStudentRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int studentId, CancellationToken cancellationToken);
}
