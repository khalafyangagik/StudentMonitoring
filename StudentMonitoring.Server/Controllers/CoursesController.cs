using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentMonitoring.Server.DTOs;
using StudentMonitoring.Server.Services;

namespace StudentMonitoring.Server.Controllers;

[ApiController]
[Authorize(Roles = "superadmin,admin,administrator,ադմինիստրատոր")]
[Route("api/[controller]")]
public sealed class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CourseResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var courses = await _courseService.GetAllAsync(cancellationToken);
        return Ok(courses);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CourseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CourseResponse>> Create(
        [FromBody] CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var course = await _courseService.CreateAsync(request, cancellationToken);
            return Created($"/api/courses/{course.Id}", course);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
