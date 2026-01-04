using System;
using System.Web.Http;
using ZKTecoApi.Services;

namespace ZKTecoApi.Controllers
{
    /// <summary>
    /// ZKTeco cihaz yönetimi ve kontrol işlemleri için endpoint'ler
    /// </summary>
    [RoutePrefix("api/device")]
    public class DeviceController : ApiController
    {
        private readonly IZKTecoSDKService _sdkService;

        public DeviceController()
        {
            _sdkService = new ZKTecoSDKService();
        }

        /// <summary>
        /// Cihazın detaylı durum bilgilerini getirir
        /// </summary>
        /// <param name="ip">Cihaz IP adresi (örn: 192.168.1.201)</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>Cihaz durum bilgileri (seri no, firmware, kapasite, kullanıcı/log sayısı vb.)</returns>
        /// <response code="200">Cihaz bilgileri başarıyla alındı</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <response code="500">Sunucu hatası</response>
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
        /// Cihazın sistem zamanını getirir
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>Cihazın mevcut sistem zamanı</returns>
        /// <response code="200">Cihaz zamanı başarıyla alındı</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
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
        /// Cihazın sistem zamanını ayarlar
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="dateTime">Ayarlanacak tarih ve saat (ISO 8601 format)</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Cihaz zamanı başarıyla ayarlandı</response>
        /// <response code="400">Cihaza bağlanılamadı veya geçersiz tarih</response>
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
        /// Cihazı etkinleştirir (kullanıcı etkileşimini açar)
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Cihaz başarıyla etkinleştirildi</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
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
        /// Cihazı devre dışı bırakır (kullanıcı etkileşimini kapatır)
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Cihaz başarıyla devre dışı bırakıldı</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <remarks>
        /// Cihaz devre dışı bırakıldığında kullanıcılar parmak izi okutamaz veya kart geçemez.
        /// Data okuma/yazma işlemleri için cihazın devre dışı bırakılması önerilir.
        /// </remarks>
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
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Cihaz yeniden başlatılıyor</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <remarks>
        /// Cihaz yeniden başlatıldıktan sonra yaklaşık 30-60 saniye beklemeniz gerekir.
        /// Yeniden başlatma işlemi sırasında tüm bağlantılar kopacaktır.
        /// </remarks>
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
        /// Cihazı kapatır (güç keser)
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Cihaz kapatılıyor</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <remarks>
        /// DİKKAT: Bu işlem cihazın gücünü tamamen keser.
        /// Cihazı tekrar açmak için fiziksel müdahale gerekebilir.
        /// </remarks>
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
