using System;
using System.Web.Http;
using ZKTecoApi.Hubs;
using ZKTecoApi.Services;

namespace ZKTecoApi.Controllers
{
    [RoutePrefix("api/realtime")]
    public class RealtimeController : ApiController
    {
        private readonly IZKTecoSDKService _sdkService;

        public RealtimeController()
        {
            _sdkService = new ZKTecoSDKService();
        }

        /// <summary>
        /// Gerçek zamanlı event dinlemeyi başlatır
        /// POST: api/realtime/{ip}/start
        /// </summary>
        [HttpPost]
        [Route("{ip}/start")]
        public IHttpActionResult StartRealtimeEvents(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.StartRealtimeEvents(ip, (eventData) =>
                {
                    // SignalR Hub üzerinden tüm dinleyicilere broadcast yap
                    RealtimeEventHub.BroadcastRealtimeEvent(eventData);
                });

                if (!result)
                {
                    _sdkService.Disconnect();
                    return BadRequest("Realtime event başlatılamadı");
                }

                return Ok(new { success = true, message = $"Realtime events started for {ip}:{port}" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Gerçek zamanlı event dinlemeyi durdurur
        /// POST: api/realtime/{ip}/stop
        /// </summary>
        [HttpPost]
        [Route("{ip}/stop")]
        public IHttpActionResult StopRealtimeEvents(string ip, int port = 4370)
        {
            try
            {
                var result = _sdkService.StopRealtimeEvents();
                _sdkService.Disconnect();

                return Ok(new { success = result, message = $"Realtime events stopped for {ip}:{port}" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
