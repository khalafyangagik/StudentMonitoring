using Npgsql;
using NpgsqlTypes;
using StudentMonitoring.Server.DTOs;

namespace StudentMonitoring.Server.Repositories;

public sealed class GroupRepository : IGroupRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public GroupRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<IReadOnlyList<GroupResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT g.id, g.name, g.course_id, c.name, g.academic_year, g.student_count
            FROM "group" AS g
            LEFT JOIN course AS c ON c.id = g.course_id
            ORDER BY g.id;
            """;

        var groups = new List<GroupResponse>();
        await using var command = _dataSource.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            groups.Add(MapGroup(reader));
        }

        return groups;
    }

    public async Task<IReadOnlyList<GroupResponse>> GetForLecturerAsync(int lecturerId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DISTINCT g.id, g.name, g.course_id, c.name, g.academic_year, g.student_count
            FROM "group" AS g
            JOIN subject_offering AS so ON so.group_id = g.id
            LEFT JOIN course AS c ON c.id = g.course_id
            WHERE so.lecturer_id = @lecturerId
            ORDER BY g.id;
            """;

        var groups = new List<GroupResponse>();
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("lecturerId", lecturerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            groups.Add(MapGroup(reader));
        }

        return groups;
    }

    public async Task<bool> CourseExistsAsync(int courseId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM course WHERE id = @courseId);";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("courseId", courseId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<GroupResponse> CreateAsync(CreateGroupRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH inserted AS (
                INSERT INTO "group" (course_id, name, academic_year, student_count)
                VALUES (@courseId, @name, @academicYear, @studentCount)
                RETURNING id, name, course_id, academic_year, student_count
            )
            SELECT i.id, i.name, i.course_id, c.name, i.academic_year, i.student_count
            FROM inserted AS i
            LEFT JOIN course AS c ON c.id = i.course_id;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        AddRequestParameters(command, request);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return MapGroup(reader);
    }

    public async Task<GroupResponse?> UpdateAsync(int groupId, CreateGroupRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            WITH updated AS (
                UPDATE "group"
                SET course_id = @courseId,
                    name = @name,
                    academic_year = @academicYear,
                    student_count = @studentCount
                WHERE id = @id
                RETURNING id, name, course_id, academic_year, student_count
            )
            SELECT u.id, u.name, u.course_id, c.name, u.academic_year, u.student_count
            FROM updated AS u
            LEFT JOIN course AS c ON c.id = u.course_id;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", groupId);
        AddRequestParameters(command, request);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapGroup(reader) : null;
    }

    public async Task<bool> DeleteAsync(int groupId, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM \"group\" WHERE id = @id;";
        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("id", groupId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddRequestParameters(NpgsqlCommand command, CreateGroupRequest request)
    {
        command.Parameters.Add("courseId", NpgsqlDbType.Integer).Value =
            (object?)request.CourseId ?? DBNull.Value;
        command.Parameters.Add("name", NpgsqlDbType.Varchar, 50).Value = request.Name;
        command.Parameters.Add("academicYear", NpgsqlDbType.Varchar, 20).Value =
            (object?)request.AcademicYear ?? DBNull.Value;
        command.Parameters.Add("studentCount", NpgsqlDbType.Integer).Value =
            (object?)request.StudentCount ?? DBNull.Value;
    }

    private static GroupResponse MapGroup(NpgsqlDataReader reader)
    {
        return new GroupResponse
        {
            Id = reader.GetInt32(0),
            Name = reader.IsDBNull(1) ? null : reader.GetString(1),
            CourseId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
            CourseName = reader.IsDBNull(3) ? null : reader.GetString(3),
            AcademicYear = reader.IsDBNull(4) ? null : reader.GetString(4),
            StudentCount = reader.IsDBNull(5) ? null : reader.GetInt32(5)
        };
    }
}
