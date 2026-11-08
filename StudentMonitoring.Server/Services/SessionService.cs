using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class SessionService : ISessionService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    {
        "lecture", "practice", "lab"
    };

    private readonly ISessionRepository _repository;
    public SessionService(ISessionRepository repository) => _repository = repository;

    public Task<IReadOnlyList<SessionResponse>> GetAllAsync(CancellationToken cancellationToken) => _repository.GetAllAsync(cancellationToken);
    public Task<IReadOnlyList<SessionOptionResponse>> GetOptionsAsync(CancellationToken cancellationToken) => _repository.GetOptionsAsync(cancellationToken);
    public Task<IReadOnlyList<LecturerOptionResponse>> GetLecturersAsync(CancellationToken cancellationToken) => _repository.GetLecturersAsync(cancellationToken);

    public async Task<SessionResponse> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        var normalized = await ValidateAsync(request, cancellationToken);
        return await _repository.CreateAsync(normalized, cancellationToken);
    }

    public async Task<SessionResponse?> UpdateAsync(int id, CreateSessionRequest request, CancellationToken cancellationToken) =>
        await _repository.UpdateAsync(id, await ValidateAsync(request, cancellationToken), cancellationToken);

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) => _repository.DeleteAsync(id, cancellationToken);

    private async Task<CreateSessionRequest> ValidateAsync(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        if (!AllowedTypes.Contains(request.Type)) throw new ArgumentException("Ընտրեք դասաժամի ճիշտ տեսակը։");
        if (request.SessionDate == default) throw new ArgumentException("Դասաժամի ամսաթիվը պարտադիր է։");
        if (request.StartTime >= request.EndTime) throw new ArgumentException("Ավարտի ժամը պետք է մեկնարկի ժամից ուշ լինի։");
        if (string.IsNullOrWhiteSpace(request.AcademicYear)) throw new ArgumentException("Ուսումնական տարին պարտադիր է։");
        if (!await _repository.SubjectAndGroupExistAsync(request.SubjectId, request.GroupId, cancellationToken))
            throw new ArgumentException("Ընտրված առարկան կամ խումբը գոյություն չունի։");
        if (request.LecturerId is int lecturerId && !await _repository.LecturerExistsAsync(lecturerId, cancellationToken))
            throw new ArgumentException("Ընտրված դասախոսը գոյություն չունի կամ ակտիվ չէ։");

        return new CreateSessionRequest
        {
            SubjectId = request.SubjectId,
            GroupId = request.GroupId,
            AcademicYear = request.AcademicYear.Trim(),
            Semester = request.Semester,
            LecturerId = request.LecturerId,
            Type = request.Type,
            SessionDate = request.SessionDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Topic = string.IsNullOrWhiteSpace(request.Topic) ? null : request.Topic.Trim(),
            Room = request.Room,
            IsCompleted = request.IsCompleted
        };
    }
}
