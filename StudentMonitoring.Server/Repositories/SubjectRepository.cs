using Npgsql;
using NpgsqlTypes;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class SubjectRepository : ISubjectRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public SubjectRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<SubjectResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.id, s.name, s.description, s.prerequisite_subject_id, prerequisite.name
            FROM subject AS s
            LEFT JOIN subject AS prerequisite ON prerequisite.id = s.prerequisite_subject_id
            ORDER BY s.id;
            """;

        var subjects = new List<SubjectResponse>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            subjects.Add(MapSubject(reader));
        }

        return subjects;
    }

    public async Task<bool> ExistsAsync(int subjectId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM subject WHERE id = @subjectId);";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("subjectId", subjectId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<SubjectResponse> CreateAsync(CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH inserted AS (
                INSERT INTO subject (name, description, prerequisite_subject_id)
                VALUES (@name, @description, @prerequisiteSubjectId)
                RETURNING id, name, description, prerequisite_subject_id
            )
            SELECT i.id, i.name, i.description, i.prerequisite_subject_id, prerequisite.name
            FROM inserted AS i
            LEFT JOIN subject AS prerequisite ON prerequisite.id = i.prerequisite_subject_id;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("name", request.Name);
        command.Parameters.Add("description", NpgsqlDbType.Text).Value =
            (object?)request.Description ?? DBNull.Value;
        command.Parameters.Add("prerequisiteSubjectId", NpgsqlDbType.Integer).Value =
            (object?)request.PrerequisiteSubjectId ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return MapSubject(reader);
    }

    public async Task<SubjectResponse?> UpdateAsync(int subjectId, CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH updated AS (
                UPDATE subject
                SET name = @name,
                    description = @description,
                    prerequisite_subject_id = @prerequisiteSubjectId
                WHERE id = @id
                RETURNING id, name, description, prerequisite_subject_id
            )
            SELECT u.id, u.name, u.description, u.prerequisite_subject_id, prerequisite.name
            FROM updated AS u
            LEFT JOIN subject AS prerequisite ON prerequisite.id = u.prerequisite_subject_id;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", subjectId);
        command.Parameters.AddWithValue("name", request.Name);
        command.Parameters.Add("description", NpgsqlDbType.Text).Value =
            (object?)request.Description ?? DBNull.Value;
        command.Parameters.Add("prerequisiteSubjectId", NpgsqlDbType.Integer).Value =
            (object?)request.PrerequisiteSubjectId ?? DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapSubject(reader) : null;
    }

    public async Task<bool> DeleteAsync(int subjectId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM subject WHERE id = @id;";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", subjectId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static SubjectResponse MapSubject(NpgsqlDataReader reader)
    {
        return new SubjectResponse
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            PrerequisiteSubjectId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            PrerequisiteSubjectName = reader.IsDBNull(4) ? null : reader.GetString(4)
        };
    }
}
