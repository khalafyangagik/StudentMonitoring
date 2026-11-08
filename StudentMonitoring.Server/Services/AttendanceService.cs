using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class AttendanceService : IAttendanceService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "present", "absent", "late"
    };

    private readonly IAttendanceRepository _repository;

    public AttendanceService(IAttendanceRepository repository) => _repository = repository;

    public Task<IReadOnlyList<AttendanceResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        _repository.GetAllAsync(cancellationToken);

    public Task<IReadOnlyList<AttendanceResponse>> GetForLecturerAsync(int lecturerId, CancellationToken cancellationToken) =>
        _repository.GetForLecturerAsync(lecturerId, cancellationToken);

    public Task<IReadOnlyList<SessionOptionResponse>> GetSessionsAsync(CancellationToken cancellationToken) =>
        _repository.GetSessionsAsync(cancellationToken);

    public Task<IReadOnlyList<SessionOptionResponse>> GetSessionsForLecturerAsync(int lecturerId, CancellationToken cancellationToken) =>
        _repository.GetSessionsForLecturerAsync(lecturerId, cancellationToken);

    public Task<IReadOnlyList<StudentResponse>> GetStudentsForLecturerAsync(int lecturerId, CancellationToken cancellationToken) =>
        _repository.GetStudentsForLecturerAsync(lecturerId, cancellationToken);

    public async Task<AttendanceResponse> CreateAsync(CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        var normalized = await ValidateAsync(request, null, cancellationToken);
        return await _repository.CreateAsync(normalized, cancellationToken);
    }

    public async Task<AttendanceResponse> CreateForLecturerAsync(int lecturerId, CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        var normalized = await ValidateAsync(request, null, cancellationToken);
        if (!await _repository.StudentCanAttendLecturerSessionAsync(lecturerId, normalized.StudentId, normalized.SessionId, cancellationToken))
            throw new ArgumentException("Կարող եք գրանցել միայն ձեր դասավանդած խմբի ակտիվ ուսանողի հաճախելիությունը։");
        return await _repository.CreateAsync(normalized, cancellationToken);
    }

    public async Task<AttendanceResponse?> UpdateAsync(int id, CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        var normalized = await ValidateAsync(request, id, cancellationToken);
        return await _repository.UpdateAsync(id, normalized, cancellationToken);
    }

    public async Task<AttendanceResponse?> UpdateForLecturerAsync(int lecturerId, int id, CreateAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (!await _repository.LecturerOwnsAttendanceAsync(lecturerId, id, cancellationToken)) return null;
        var normalized = await ValidateAsync(request, id, cancellationToken);
        if (!await _repository.StudentCanAttendLecturerSessionAsync(lecturerId, normalized.StudentId, normalized.SessionId, cancellationToken))
            throw new ArgumentException("Կարող եք գրանցել միայն ձեր դասավանդած խմբի ակտիվ ուսանողի հաճախելիությունը։");
        return await _repository.UpdateAsync(id, normalized, cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        _repository.DeleteAsync(id, cancellationToken);

    public async Task<bool> DeleteForLecturerAsync(int lecturerId, int id, CancellationToken cancellationToken) =>
        await _repository.LecturerOwnsAttendanceAsync(lecturerId, id, cancellationToken)
        && await _repository.DeleteAsync(id, cancellationToken);

    private async Task<CreateAttendanceRequest> ValidateAsync(CreateAttendanceRequest request, int? excludingId,
        CancellationToken cancellationToken)
    {
        if (!AllowedStatuses.Contains(request.Status))
            throw new ArgumentException("Ընտրեք հաճախելիության ճիշտ կարգավիճակ։");
        if (!await _repository.StudentCanAttendSessionAsync(request.StudentId, request.SessionId, cancellationToken))
            throw new ArgumentException("Ուսանողը գրանցված չէ այս դասաժամի խմբում։");
        if (await _repository.AttendanceExistsAsync(request.StudentId, request.SessionId, excludingId, cancellationToken))
            throw new ArgumentException("Այս ուսանողի հաճախելիությունն արդեն գրանցված է տվյալ դասաժամի համար։");

        return new CreateAttendanceRequest
        {
            StudentId = request.StudentId,
            SessionId = request.SessionId,
            Status = request.Status,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim()
        };
    }
}
