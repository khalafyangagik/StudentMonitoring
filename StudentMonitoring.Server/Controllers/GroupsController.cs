using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Security.Claims;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "superadmin,admin,administrator,ադմինիստրատոր,lecturer,teacher,instructor,դասախոս")]
[Route("api/[controller]")]
public sealed class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GroupResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GroupResponse>>> GetAll(CancellationToken cancellationToken)
    {
        if (User.IsInRole("lecturer") || User.IsInRole("teacher") || User.IsInRole("instructor") || User.IsInRole("դասախոս"))
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var lecturerId)) return Unauthorized();
            return Ok(await _groupService.GetForLecturerAsync(lecturerId, cancellationToken));
        }

        return Ok(await _groupService.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "superadmin,admin,administrator,ադմինիստրատոր")]
    [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GroupResponse>> Create(
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var group = await _groupService.CreateAsync(request, cancellationToken);
            return Created($"/api/groups/{group.Id}", group);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "superadmin,admin,administrator,ադմինիստրատոր")]
    [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GroupResponse>> Update(
        int id,
        [FromBody] CreateGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var group = await _groupService.UpdateAsync(id, request, cancellationToken);
            return group is null ? NotFound() : Ok(group);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "superadmin,admin,administrator,ադմինիստրատոր")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _groupService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Conflict(new { message = "This group has subject offerings and cannot be deleted." });
        }
    }
}
