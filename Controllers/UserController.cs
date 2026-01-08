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
                    return Ok(new { success = false, message = $"Cihaza bağlanılamadı: {ip}:{port}" });
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
                    return Ok(new { success = false, message = $"Cihaza bağlanılamadı: {ip}:{port}" });
                }

                var user = _sdkService.GetUser(enrollNumber);
                _sdkService.Disconnect();

                if (user == null)
                {
                    return Ok(new { success = false, message = $"Kullanıcı bulunamadı: {enrollNumber}" });
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
        /// <returns>Kullanıcı listesi (bir kart birden fazla kullanıcıda olabilir)</returns>
        /// <response code="200">Kullanıcı(lar) bulundu</response>
        /// <response code="404">Bu kart numarasına sahip kullanıcı bulunamadı</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <remarks>
        /// Bu endpoint tüm kullanıcıları çeker ve kart numarasına göre filtreler.
        /// Bir kart numarası birden fazla kullanıcıda kayıtlı olabilir, bu yüzden liste döndürür.
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
                    return Ok(new { success = false, message = $"Cihaza bağlanılamadı: {ip}:{port}" });
                }

                // Tüm kullanıcıları çek ve kart numarasıyla filtrele
                var allUsers = _sdkService.GetAllUsers();
                var users = allUsers.Where(u => u.CardNumber == cardNumber).ToList();

                _sdkService.Disconnect();

                if (users.Count == 0)
                {
                    return Ok(new { success = false, message = $"Kart numarası {cardNumber} ile kullanıcı bulunamadı", data = new object[] { }, count = 0 });
                }

                return Ok(new
                {
                    success = true,
                    data = users,
                    count = users.Count,
                    message = users.Count > 1 ? $"Bu kart numarası {users.Count} kullanıcıda kayıtlı" : "Kullanıcı bulundu"
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
        /// <param name="force">Kart çakışması varsa otomatik temizle (varsayılan: false)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Kullanıcı başarıyla oluşturuldu</response>
        /// <response code="400">Cihaza bağlanılamadı veya geçersiz request</response>
        /// <response code="409">Kart çakışması var (force=false ise)</response>
        /// <remarks>
        /// Privilege değerleri:
        /// - 0: User (normal kullanıcı)
        /// - 1: Enroller (kayıt yetkisi)
        /// - 2: Manager (yönetici)
        /// - 3: Super Admin (süper yönetici)
        ///
        /// force parametresi:
        /// - false (varsayılan): Kart başka kullanıcıda varsa hata döner
        /// - true: Kart başka kullanıcıda varsa otomatik temizler ve devam eder
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
        public IHttpActionResult CreateUser(string ip, [FromBody] UserCreateRequest request, int port = 4370, bool force = false)
        {
            try
            {
                if (request == null)
                {
                    return Ok(new { success = false, message = "Request body gerekli" });
                }

                if (!_sdkService.Connect(ip, port))
                {
                    return Ok(new { success = false, message = $"Cihaza bağlanılamadı: {ip}:{port}" });
                }

                // Kart numarası kontrolü
                if (request.CardNumber.HasValue && request.CardNumber.Value > 0)
                {
                    var allUsers = _sdkService.GetAllUsers();
                    var conflictingUsers = allUsers.Where(u => u.CardNumber == request.CardNumber.Value).ToList();

                    if (conflictingUsers.Any())
                    {
                        if (!force)
                        {
                            // force=false: Hata döndür
                            _sdkService.Disconnect();
                            return Ok(new
                            {
                                success = false,
                                conflict = true,
                                message = $"Bu kart numarası {conflictingUsers.Count} kullanıcıda kayıtlı. force=true ile devam edebilirsiniz.",
                                conflictingUsers = conflictingUsers.Select(u => new
                                {
                                    enrollNumber = u.EnrollNumber,
                                    name = u.Name,
                                    cardNumber = u.CardNumber
                                }).ToList()
                            });
                        }
                        else
                        {
                            // force=true: Eski kayıtları temizle
                            foreach (var user in conflictingUsers)
                            {
                                var updateRequest = new UserUpdateRequest
                                {
                                    Name = user.Name,
                                    Password = user.Password,
                                    CardNumber = 0,
                                    Privilege = user.Privilege,
                                    Enabled = user.Enabled
                                };
                                _sdkService.UpdateUser(user.EnrollNumber, updateRequest);
                            }
                        }
                    }
                }

                var result = _sdkService.CreateUser(request);
                _sdkService.Disconnect();

                return Ok(new { success = result, message = result ? "Kullanıcı oluşturuldu" : "Kullanıcı oluşturulamadı" });
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
        /// <param name="force">Kart çakışması varsa otomatik temizle (varsayılan: false)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Kullanıcı başarıyla güncellendi</response>
        /// <response code="400">Cihaza bağlanılamadı veya kullanıcı bulunamadı</response>
        /// <response code="409">Kart çakışması var (force=false ise)</response>
        /// <remarks>
        /// Sadece gönderilen alanlar güncellenir. Null olanlar mevcut değerini korur.
        ///
        /// force parametresi:
        /// - false (varsayılan): Kart başka kullanıcıda varsa hata döner
        /// - true: Kart başka kullanıcıda varsa otomatik temizler ve devam eder
        ///
        /// Örnek request:
        /// {
        ///   "name": "Ahmet Yılmaz (Güncellendi)",
        ///   "cardNumber": 123456789,
        ///   "privilege": 2
        /// }
        /// </remarks>
        [HttpPut]
        [Route("{ip}/{enrollNumber}")]
        public IHttpActionResult UpdateUser(string ip, string enrollNumber, [FromBody] UserUpdateRequest request, int port = 4370, bool force = false)
        {
            try
            {
                if (request == null)
                {
                    return Ok(new { success = false, message = "Request body gerekli" });
                }

                if (!_sdkService.Connect(ip, port))
                {
                    return Ok(new { success = false, message = $"Cihaza bağlanılamadı: {ip}:{port}" });
                }

                // Kart numarası kontrolü
                if (request.CardNumber.HasValue && request.CardNumber.Value > 0)
                {
                    var allUsers = _sdkService.GetAllUsers();
                    var conflictingUsers = allUsers.Where(u => 
                        u.CardNumber == request.CardNumber.Value && 
                        u.EnrollNumber != enrollNumber // Kendisi hariç
                    ).ToList();

                    if (conflictingUsers.Any())
                    {
                        if (!force)
                        {
                            // force=false: Hata döndür
                            _sdkService.Disconnect();
                            return Ok(new
                            {
                                success = false,
                                conflict = true,
                                message = $"Bu kart numarası {conflictingUsers.Count} kullanıcıda kayıtlı. force=true ile devam edebilirsiniz.",
                                conflictingUsers = conflictingUsers.Select(u => new
                                {
                                    enrollNumber = u.EnrollNumber,
                                    name = u.Name,
                                    cardNumber = u.CardNumber
                                }).ToList()
                            });
                        }
                        else
                        {
                            // force=true: Eski kayıtları temizle
                            foreach (var user in conflictingUsers)
                            {
                                var updateRequest = new UserUpdateRequest
                                {
                                    Name = user.Name,
                                    Password = user.Password,
                                    CardNumber = 0,
                                    Privilege = user.Privilege,
                                    Enabled = user.Enabled
                                };
                                _sdkService.UpdateUser(user.EnrollNumber, updateRequest);
                            }
                        }
                    }
                }

                var result = _sdkService.UpdateUser(enrollNumber, request);
                _sdkService.Disconnect();

                return Ok(new { success = result, message = result ? "Kullanıcı güncellendi" : "Kullanıcı güncellenemedi" });
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
                    return Ok(new { success = false, message = $"Cihaza bağlanılamadı: {ip}:{port}" });
                }

                var result = _sdkService.DeleteUser(enrollNumber);
                _sdkService.Disconnect();

                return Ok(new { success = result, message = result ? "Kullanıcı silindi" : "Kullanıcı silinemedi" });
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
                    return Ok(new { success = false, message = $"Cihaza bağlanılamadı: {ip}:{port}" });
                }

                var result = _sdkService.ClearAllUsers();
                _sdkService.Disconnect();

                return Ok(new { success = result, message = result ? "Tüm kullanıcılar silindi" : "Kullanıcılar silinemedi" });
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
                    return Ok(new { success = false, message = $"Cihaza bağlanılamadı: {ip}:{port}" });
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

        /// <summary>
        /// GÜVENLİ Geçici kart yönetimi: Belirli bir kullanıcıdan kart numarasını kaldırır
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="enrollNumber">Kart numarasını kaldırılacak kullanıcının EnrollNumber'ı</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Kart numarası temizlendi</response>
        /// <response code="400">Cihaza bağlanılamadı veya kullanıcı bulunamadı</response>
        /// <remarks>
        /// GÜVENLİ YÖNTEM: Sadece belirtilen kullanıcıdan kart numarasını kaldırır.
        /// Yanlışlıkla başka kullanıcıların kartlarını silme riski yoktur.
        /// Kullanım: DELETE /api/users/{ip}/{enrollNumber}/card
        /// </remarks>
        [HttpDelete]
        [Route("{ip}/{enrollNumber}/card")]
        public IHttpActionResult RemoveCardFromUser(string ip, string enrollNumber, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return Ok(new { success = false, message = $"Cihaza bağlanılamadı: {ip}:{port}" });
                }

                // Kullanıcıyı bul
                var user = _sdkService.GetUser(enrollNumber);
                if (user == null)
                {
                    _sdkService.Disconnect();
                    return Ok(new { success = false, message = $"Kullanıcı bulunamadı: {enrollNumber}" });
                }

                var oldCardNumber = user.CardNumber;

                // Kullanıcının kart numarasını sıfırla
                var updateRequest = new UserUpdateRequest
                {
                    Name = user.Name,
                    Password = user.Password,
                    CardNumber = 0, // Kart numarasını sıfırla
                    Privilege = user.Privilege,
                    Enabled = user.Enabled
                };

                bool result = _sdkService.UpdateUser(enrollNumber, updateRequest);
                _sdkService.Disconnect();

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Kart numarası {oldCardNumber}, kullanıcı {enrollNumber} ({user.Name}) üzerinden kaldırıldı",
                        removedFrom = new
                        {
                            enrollNumber = enrollNumber,
                            name = user.Name,
                            oldCardNumber = oldCardNumber
                        }
                    });
                }
                else
                {
                    return Ok(new { success = false, message = "Kart numarası kaldırılamadı" });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// TEHLİKELİ: Belirtilen kart numarasını TÜM kullanıcılardan kaldırır
        /// </summary>
        /// <param name="ip">Cihaz IP adresi</param>
        /// <param name="cardNumber">Temizlenecek kart numarası</param>
        /// <param name="port">Cihaz port numarası (varsayılan: 4370)</param>
        /// <returns>İşlem sonucu</returns>
        /// <response code="200">Kart numarası temizlendi</response>
        /// <response code="400">Cihaza bağlanılamadı</response>
        /// <remarks>
        /// ⚠️ TEHLİKELİ: Bu endpoint tüm kullanıcılardan kart numarasını siler!
        /// Yanlış kart numarası girilirse gerçek kullanıcıların kartları silinebilir.
        /// Bunun yerine DELETE /api/users/{ip}/{enrollNumber}/card kullanın.
        /// </remarks>
        [HttpDelete]
        [Route("{ip}/card/{cardNumber}/clear-all")]
        public IHttpActionResult ClearCardNumberFromAllUsers(string ip, long cardNumber, int port = 4370)
        {
            try
            {
                if (!_sdkService.Connect(ip, port))
                {
                    return Ok(new { success = false, message = $"Cihaza bağlanılamadı: {ip}:{port}" });
                }

                // Tüm kullanıcıları çek ve bu kart numarasına sahip olanları bul
                var allUsers = _sdkService.GetAllUsers();
                var usersWithCard = allUsers.Where(u => u.CardNumber == cardNumber).ToList();

                if (usersWithCard.Count == 0)
                {
                    _sdkService.Disconnect();
                    return Ok(new { success = true, message = $"Kart numarası {cardNumber} hiçbir kullanıcıda kayıtlı değil", clearedCount = 0 });
                }

                int clearedCount = 0;
                var clearedUsers = new System.Collections.Generic.List<object>();

                foreach (var user in usersWithCard)
                {
                    // Kullanıcının kart numarasını sıfırla (0 yap)
                    var updateRequest = new UserUpdateRequest
                    {
                        Name = user.Name,
                        Password = user.Password,
                        CardNumber = 0, // Kart numarasını sıfırla
                        Privilege = user.Privilege,
                        Enabled = user.Enabled
                    };

                    if (_sdkService.UpdateUser(user.EnrollNumber, updateRequest))
                    {
                        clearedCount++;
                        clearedUsers.Add(new
                        {
                            enrollNumber = user.EnrollNumber,
                            name = user.Name,
                            oldCardNumber = cardNumber
                        });
                    }
                }

                _sdkService.Disconnect();

                return Ok(new
                {
                    success = true,
                    message = $"Kart numarası {cardNumber}, {clearedCount} kullanıcıdan temizlendi",
                    clearedCount = clearedCount,
                    clearedUsers = clearedUsers
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
