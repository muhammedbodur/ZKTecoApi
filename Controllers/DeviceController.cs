using System;
using System.Web.Http;
using ZKTecoApi.Services;

namespace ZKTecoApi.Controllers
{
    [RoutePrefix("api/device")]
    public class DeviceController : ApiController
    {
        private readonly IZKTecoSDKService _sdkService;

        public DeviceController()
        {
            _sdkService = new ZKTecoSDKService();
        }

        /// <summary>
        /// Cihaz durumunu getirir
        /// GET: api/device/{ip}/status
        /// </summary>
        [HttpGet]
        [Route("{ip}/status")]
        public IHttpActionResult GetStatus(string ip, int port = 4370)
        {
            try
            {
                var status = _sdkService.GetDeviceStatus(ip, port);
                return Ok(new { success = true, data = status });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cihaz zamanını getirir
        /// GET: api/device/{ip}/time
        /// </summary>
        [HttpGet]
        [Route("{ip}/time")]
        public IHttpActionResult GetDeviceTime(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var deviceTime = _sdkService.GetDeviceTime();
                _sdkService.Disconnect();

                return Ok(new { success = true, data = new { deviceTime } });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cihaz zamanını ayarlar
        /// POST: api/device/{ip}/time
        /// </summary>
        [HttpPost]
        [Route("{ip}/time")]
        public IHttpActionResult SetDeviceTime(string ip, [FromBody] DateTime dateTime, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.SetDeviceTime(dateTime);
                _sdkService.Disconnect();

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cihazı etkinleştirir
        /// POST: api/device/{ip}/enable
        /// </summary>
        [HttpPost]
        [Route("{ip}/enable")]
        public IHttpActionResult EnableDevice(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.EnableDevice();
                _sdkService.Disconnect();

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cihazı devre dışı bırakır
        /// POST: api/device/{ip}/disable
        /// </summary>
        [HttpPost]
        [Route("{ip}/disable")]
        public IHttpActionResult DisableDevice(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.DisableDevice();
                _sdkService.Disconnect();

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cihazı yeniden başlatır
        /// POST: api/device/{ip}/restart
        /// </summary>
        [HttpPost]
        [Route("{ip}/restart")]
        public IHttpActionResult RestartDevice(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.Restart();
                _sdkService.Disconnect();

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cihazı kapatır
        /// POST: api/device/{ip}/poweroff
        /// </summary>
        [HttpPost]
        [Route("{ip}/poweroff")]
        public IHttpActionResult PowerOffDevice(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.PowerOff();
                _sdkService.Disconnect();

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
