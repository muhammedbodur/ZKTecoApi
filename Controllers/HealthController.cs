using System;
using System.Web.Http;

namespace ZKTecoApi.Controllers
{
    [RoutePrefix("api/health")]
    public class HealthController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.Now });
        }
    }
}
