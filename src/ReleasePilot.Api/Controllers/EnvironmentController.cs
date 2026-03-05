using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ReleasePilot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EnvironmentController : ControllerBase
    {
        [HttpGet]
        [Route("{id}")]
        public IActionResult GetById(int id)
        {
            return Ok($"EnvironmentController is working! ID: {id}");
        }
    }
}
