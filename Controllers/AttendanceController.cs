using System;
using System.Web.Http;
using ZKTecoApi.Services;

namespace ZKTecoApi.Controllers
{
    [RoutePrefix("api/attendance")]
    public class AttendanceController : ApiController
    {
        private readonly IZKTecoSDKService _sdkService;

        public AttendanceController()
        {
            _sdkService = new ZKTecoSDKService();
        }

        /// <summary>
        /// Tüm yoklama kayıtlarını getirir
        /// GET: api/attendance/{ip}
        /// </summary>
        [HttpGet]
        [Route("{ip}")]
        public IHttpActionResult GetAttendanceLogs(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var logs = _sdkService.GetAttendanceLogs(ip);
                _sdkService.Disconnect();

                return Ok(new { success = true, data = logs, count = logs.Count });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Tüm yoklama kayıtlarını siler
        /// DELETE: api/attendance/{ip}
        /// </summary>
        [HttpDelete]
        [Route("{ip}")]
        public IHttpActionResult ClearAttendanceLogs(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.ClearAttendanceLogs();
                _sdkService.Disconnect();

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Yoklama kayıt sayısını getirir
        /// GET: api/attendance/{ip}/count
        /// </summary>
        [HttpGet]
        [Route("{ip}/count")]
        public IHttpActionResult GetLogCount(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var count = _sdkService.GetLogCount();
                _sdkService.Disconnect();

                return Ok(new { success = true, data = new { logCount = count } });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
