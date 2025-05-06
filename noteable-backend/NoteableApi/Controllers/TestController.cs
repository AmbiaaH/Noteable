using Microsoft.AspNetCore.Mvc;

namespace NoteableApi.Controllers
{
    [ApiController]
    [Route("test")]
    // Basic test controller to verify that the API is up and responding to HTTP requests.

    public class TestController : ControllerBase
    {
        [HttpGet] // Returns a simple success message. 
                 // Used for quick connectivity tests.

        public IActionResult Get() => Ok("Test controller works");
    }
}