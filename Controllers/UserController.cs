using System;
using System.Linq;
using System.Web.Http;
using ZKTecoApi.DTOs.Request;
using ZKTecoApi.Services;

namespace ZKTecoApi.Controllers
{
    /// <summary>
    /// ZKTeco cihazındaki kullanıcı yönetimi işlemleri için endpoint'ler
    /// </summary>
    [RoutePrefix("api/users")]
    public class UserController : ApiController
    {
        private readonly IZKTecoSDKService _sdkService;

        public UserController()
        {
            _sdkService = new ZKTecoSDKService();
        }

        /// <summary>
        /// Cihazda kayıtlı tüm kullanıcıları getirir
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>Kullanıcı listesi (enrollNumber, name, cardNumber, privilege, enabled)</returns>
        /// <response code="200">Kullanıcı listesi başarıyla alındı</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <response code="500">Sunucu hatası</response>
        [HttpGet]
        [Route("{ip}")]
        public IHttpActionResult GetAllUsers(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var users = _sdkService.GetAllUsers();
                _sdkService.Disconnect();

                return Ok(new { success = true, data = users, count = users.Count });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Belirli bir kullanıcının bilgilerini getirir
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="enrollNumber">Kullanıcı kayıt numarası (örn: "1001")</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>Kullanıcı bilgileri</returns>
        /// <response code="200">Kullanıcı bilgileri başarıyla alındı</response>
        /// <response code="404">Kullanıcı bulunamadı</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        [HttpGet]
        [Route("{ip}/{enrollNumber}")]
        public IHttpActionResult GetUser(string ip, string enrollNumber, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var user = _sdkService.GetUser(enrollNumber);
                _sdkService.Disconnect();

                if (user == null)
                {
                    return NotFound();
                }

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Kart numarası ile kullanıcı arar
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="cardNumber">Kullanıcı kart numarası (örn: 123456789)</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>Kullanıcı bilgileri</returns>
        /// <response code="200">Kullanıcı bulundu</response>
        /// <response code="404">Bu kart numarasına sahip kullanıcı bulunamadı</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <remarks>
        /// Bu endpoint tüm kullanıcıları çeker ve kart numarasına göre filtreler.
        /// Büyük kullanıcı listeleri için performans sorunu yaşanabilir.
        /// </remarks>
        [HttpGet]
        [Route("{ip}/card/{cardNumber}")]
        public IHttpActionResult GetUserByCardNumber(string ip, long cardNumber, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                // Tüm kullanıcıları çek ve kart numarasıyla filtrele
                var allUsers = _sdkService.GetAllUsers();
                var user = allUsers.FirstOrDefault(u => u.CardNumber == cardNumber);

                _sdkService.Disconnect();

                if (user == null)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    success = true,
                    data = user
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cihaza yeni kullanıcı ekler
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="request">Kullanıcı bilgileri (enrollNumber, name, password, cardNumber, privilege, enabled)</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Kullanıcı başarıyla oluşturuldu</response>
        /// <response code="400">Cihaza bağlanılamadı veya geçersiz request</response>
        /// <remarks>
        /// Privilege değerleri:
        /// - 0: User (normal kullanıcı)
        /// - 1: Enroller (kayıt yetkisi)
        /// - 2: Manager (yönetici)
        /// - 3: Super Admin (süper yönetici)
        ///
        /// Örnek request:
        /// {
        ///   "enrollNumber": "1001",
        ///   "name": "Ahmet Yılmaz",
        ///   "password": "1234",
        ///   "cardNumber": 123456789,
        ///   "privilege": 0,
        ///   "enabled": true
        /// }
        /// </remarks>
        [HttpPost]
        [Route("{ip}")]
        public IHttpActionResult CreateUser(string ip, [FromBody] UserCreateRequest request, int port = 4370)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body gerekli");
                }

                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.CreateUser(request);
                _sdkService.Disconnect();

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Kullanıcı bilgilerini günceller
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="enrollNumber">Güncellenecek kullanıcının kayıt numarası</param>
        /// <param name="request">Güncellenecek alanlar (null olanlar değiştirilmez)</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Kullanıcı başarıyla güncellendi</response>
        /// <response code="400">Cihaza bağlanılamadı veya kullanıcı bulunamadı</response>
        /// <remarks>
        /// Sadece gönderilen alanlar güncellenir. Null olanlar mevcut değerini korur.
        ///
        /// Örnek request:
        /// {
        ///   "name": "Ahmet Yılmaz (Güncellendi)",
        ///   "privilege": 2
        /// }
        /// </remarks>
        [HttpPut]
        [Route("{ip}/{enrollNumber}")]
        public IHttpActionResult UpdateUser(string ip, string enrollNumber, [FromBody] UserUpdateRequest request, int port = 4370)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body gerekli");
                }

                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.UpdateUser(enrollNumber, request);
                _sdkService.Disconnect();

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Kullanıcıyı cihazdan siler
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="enrollNumber">Silinecek kullanıcının kayıt numarası</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Kullanıcı başarıyla silindi</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <remarks>
        /// DİKKAT: Bu işlem kullanıcının tüm verilerini (parmak izi, kart, vb.) siler.
        /// İşlem geri alınamaz!
        /// </remarks>
        [HttpDelete]
        [Route("{ip}/{enrollNumber}")]
        public IHttpActionResult DeleteUser(string ip, string enrollNumber, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.DeleteUser(enrollNumber);
                _sdkService.Disconnect();

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cihazdan tüm kullanıcıları siler
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Tüm kullanıcılar başarıyla silindi</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <remarks>
        /// ⚠️ UYARI: Bu işlem cihazdan TÜM kullanıcıları siler!
        /// Tüm parmak izi, kart ve kullanıcı bilgileri kalıcı olarak silinir.
        /// Bu işlem geri alınamaz! Dikkatli kullanın.
        /// </remarks>
        [HttpDelete]
        [Route("{ip}")]
        public IHttpActionResult ClearAllUsers(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var result = _sdkService.ClearAllUsers();
                _sdkService.Disconnect();

                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cihazda kayıtlı toplam kullanıcı sayısını getirir
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>Kullanıcı sayısı</returns>
        /// <response code="200">Kullanıcı sayısı başarıyla alındı</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        [HttpGet]
        [Route("{ip}/count")]
        public IHttpActionResult GetUserCount(string ip, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return BadRequest($"Cihaza bağlanılamadı: {ip}:{port}");
                }

                var count = _sdkService.GetUserCount();
                _sdkService.Disconnect();

                return Ok(new { success = true, data = new { userCount = count } });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
