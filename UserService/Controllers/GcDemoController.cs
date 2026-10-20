using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/gc-demo")]
    public class GcDemoController : ControllerBase
    {
        private static readonly List<byte[]> RetainedObjects = [];

        [HttpGet]
        public IActionResult Get()
        {
            // A new object, normally allocated in Gen 0.
            var temporary = new byte[1024];

            // Keep this object alive across requests.
            var retained = new byte[1024];
            RetainedObjects.Add(retained);

            var initialGeneration = GC.GetGeneration(retained);

            // DEMONSTRATION ONLY: force a Gen 0 collection.
            GC.Collect(0);
            var afterGen0 = GC.GetGeneration(retained);

            // DEMONSTRATION ONLY: force collection through Gen 1.
            GC.Collect(1);
            var afterGen1 = GC.GetGeneration(retained);

            GC.KeepAlive(temporary);
            GC.KeepAlive(retained);

            return Ok(new
            {
                InitialGeneration = initialGeneration,
                AfterGen0Collection = afterGen0,
                AfterGen1Collection = afterGen1,
                TemporaryGeneration = GC.GetGeneration(temporary)
            });
        }
    }
}
