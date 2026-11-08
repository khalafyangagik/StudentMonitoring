using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Repositories;

namespace StudentMonitoring.Server.Services;

public sealed class StudentService : IStudentService
{
    private readonly IStudentRepository _repository;

    public StudentService(IStudentRepository repository) => _repository = repository;

    public Task<IReadOnlyList<StudentResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        _repository.GetAllAsync(cancellationToken);

    public async Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken) =>
        await _repository.CreateAsync(await ValidateAsync(request, cancellationToken), cancellationToken);

    public async Task<StudentResponse?> UpdateAsync(int studentId, CreateStudentRequest request, CancellationToken cancellationToken) =>
        await _repository.UpdateAsync(studentId, await ValidateAsync(request, cancellationToken), cancellationToken);

    public Task<bool> DeleteAsync(int studentId, CancellationToken cancellationToken) =>
        _repository.DeleteAsync(studentId, cancellationToken);

    private async Task<CreateStudentRequest> ValidateAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Surname))
            throw new ArgumentException("Անունն ու ազգանունը պարտադիր են։");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Էլ․ հասցեն պարտադիր է։");
        if (!await _repository.GroupExistsAsync(request.GroupId, cancellationToken))
            throw new ArgumentException("Ընտրված խումբը գոյություն չունի։");

        return new CreateStudentRequest
        {
            Name = request.Name.Trim(),
            Surname = request.Surname.Trim(),
            Email = request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            GroupId = request.GroupId
        };
    }
}
