using System;
using System.Web.Http;
using ZKTecoApi.Hubs;
using ZKTecoApi.Services;

namespace ZKTecoApi.Controllers
{
    /// <summary>
    /// ZKTeco cihazından gerçek zamanlı event'leri dinlemek için endpoint'ler
    /// </summary>
    [RoutePrefix("api/realtime")]
    public class RealtimeController : ApiController
    {
        private readonly IZKTecoSDKService _sdkService;

        public RealtimeController()
        {
            _sdkService = new ZKTecoSDKService();
        }

        /// <summary>
        /// Cihazdan gerçek zamanlı event dinlemeyi başlatır
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Realtime event dinleme başlatıldı</response>
        /// <response code="400">Cihaza bağlanılamadı veya event başlatılamadı</response>
        /// <remarks>
        /// Bu endpoint'i çağırdıktan sonra cihazdan gelen event'ler SignalR üzerinden broadcast edilir.
        /// Event'leri almak için SignalR Hub'a bağlanıp SubscribeToDevice(deviceIp) metodunu çağırmanız gerekir.
        ///
        /// Event flow:
        /// 1. POST /api/realtime/{ip}/start - Event dinlemeyi başlat
        /// 2. SignalR Hub'a bağlan (ws://server/signalr)
        /// 3. SubscribeToDevice(deviceIp) - Cihaza abone ol
        /// 4. onRealtimeEvent - Event'leri al
        ///
        /// Realtime event'ler şunları içerir:
        /// - Kart okutma
        /// - Parmak izi okutma
        /// - Yüz tanıma
        /// - Giriş/çıkış kayıtları
        /// </remarks>
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
        /// Cihazdan gerçek zamanlı event dinlemeyi durdurur
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Realtime event dinleme durduruldu</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <remarks>
        /// Bu işlem cihazdan event dinlemeyi durdurur ve cihaz bağlantısını koparır.
        /// SignalR abonelikleri otomatik olarak temizlenmez, client'ların UnsubscribeFromDevice çağırması önerilir.
        /// </remarks>
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
