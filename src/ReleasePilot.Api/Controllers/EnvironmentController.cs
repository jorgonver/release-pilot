using Microsoft.AspNetCore.Mvc;

namespace ReleasePilot.Api.Controllers;

[Route("api/environments")]
[ApiController]
public class EnvironmentController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll()
    {
        // Baseline environments until environment management rules are specified.
        var environments = new[] { "dev", "staging", "production" };
        return Ok(environments);
    }

    [HttpGet("{name}")]
    public IActionResult GetByName(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        if (normalized == "development")
        {
            normalized = "dev";
        }

        var known = new HashSet<string> { "dev", "staging", "production" };
        if (!known.Contains(normalized))
        {
            return NotFound(new { message = $"Environment '{name}' not found." });
        }

        return Ok(new { name = normalized });
    }
}
