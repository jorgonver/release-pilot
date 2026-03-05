using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ReleasePilot.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        [HttpGet]
        [Route("{id}")]
        public IActionResult GetById(int id)
        {
            return Ok($"PromotionController is working! ID: {id}");
        }

        [HttpPost]
        [Route("request")]
        public new IActionResult Request()
        {
            return Ok("Promotion request received!");
        }

        [HttpPost]
        [Route("approve")]
        public IActionResult Approve()
        {
            return Ok("Promotion approved!");
        }
        [HttpPost]
        [Route("start")]
        public IActionResult Start()
        {
            return Ok("Promotion started!");
        }

        [HttpPost]
        [Route("complete")]
        public IActionResult Complete()
        {
            return Ok("Promotion completed!");
        }

        [HttpPost]
        [Route("rollback")]
        public IActionResult Rollback()
        {
            return Ok("Promotion rolled back!");
        }

        [HttpPost]
        [Route("cancel")]
        public IActionResult Cancel()
        {
            return Ok("Promotion canceled!");
        }
    }
}
