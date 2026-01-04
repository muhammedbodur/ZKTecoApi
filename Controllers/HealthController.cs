using System;
using System.Web.Http;

namespace ZKTecoApi.Controllers
{
    /// <summary>
    /// API sağlık durumu kontrolü için endpoint'ler
    /// </summary>
    [RoutePrefix("api/health")]
    public class HealthController : ApiController
    {
        /// <summary>
        /// API'nin sağlık durumunu kontrol eder
        /// </summary>
        /// <returns>API'nin çalışma durumu ve zaman bilgisi</returns>
        /// <response code="200">API sağlıklı çalışıyor</response>
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.Now });
        }
    }
}
