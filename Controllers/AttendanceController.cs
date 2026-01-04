using System;
using System.Web.Http;
using ZKTecoApi.Services;

namespace ZKTecoApi.Controllers
{
    /// <summary>
    /// ZKTeco cihazındaki yoklama/attendance kayıtları yönetimi için endpoint'ler
    /// </summary>
    [RoutePrefix("api/attendance")]
    public class AttendanceController : ApiController
    {
        private readonly IZKTecoSDKService _sdkService;

        public AttendanceController()
        {
            _sdkService = new ZKTecoSDKService();
        }

        /// <summary>
        /// Cihazdan tüm yoklama kayıtlarını getirir
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>Yoklama kayıtları listesi (enrollNumber, dateTime, verifyMethod, inOutMode, workCode)</returns>
        /// <response code="200">Yoklama kayıtları başarıyla alındı</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <response code="500">Sunucu hatası</response>
        /// <remarks>
        /// VerifyMethod değerleri:
        /// - 0: Password (şifre)
        /// - 1: Fingerprint (parmak izi)
        /// - 15: Card (kart)
        /// - 25: Face (yüz tanıma)
        ///
        /// InOutMode (AttendanceState) değerleri:
        /// - 0: CheckIn (giriş)
        /// - 1: CheckOut (çıkış)
        /// - 2: BreakOut (mola başlangıcı)
        /// - 3: BreakIn (mola bitişi)
        /// - 4: OTIn (fazla mesai başlangıcı)
        /// - 5: OTOut (fazla mesai bitişi)
        /// </remarks>
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
        /// Cihazdan tüm yoklama kayıtlarını siler
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Yoklama kayıtları başarıyla silindi</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <remarks>
        /// ⚠️ DİKKAT: Bu işlem cihazdan TÜM yoklama kayıtlarını siler!
        /// Tüm attendance log verisi kalıcı olarak silinir.
        /// Bu işlem geri alınamaz! Kayıtları silmeden önce mutlaka yedekleyin.
        /// </remarks>
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
        /// Cihazda kayıtlı toplam yoklama kaydı sayısını getirir
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>Yoklama kayıt sayısı</returns>
        /// <response code="200">Kayıt sayısı başarıyla alındı</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
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
