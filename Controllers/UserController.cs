using System;
using System.Linq;
using System.Web.Http;
using ZKTecoApi.DTOs.Request;
using ZKTecoApi.Services;

namespace ZKTecoApi.Controllers
{
    [RoutePrefix("api/users")]
    public class UserController : ApiController
    {
        private readonly IZKTecoSDKService _sdkService;

        public UserController()
        {
            _sdkService = new ZKTecoSDKService();
        }

        /// <summary>
        /// Tüm kullanıcıları getirir
        /// GET: api/users/{ip}
        /// </summary>
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
        /// Belirli bir kullanıcıyı getirir
        /// GET: api/users/{ip}/{enrollNumber}
        /// </summary>
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
        /// Kart numarasıyla kullanıcıyı bul
        /// GET: api/users/{ip}/card/{cardNumber}
        /// </summary>
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
        /// Yeni kullanıcı oluşturur
        /// POST: api/users/{ip}
        /// </summary>
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
        /// PUT: api/users/{ip}/{enrollNumber}
        /// </summary>
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
        /// Kullanıcıyı siler
        /// DELETE: api/users/{ip}/{enrollNumber}
        /// </summary>
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
        /// Tüm kullanıcıları siler
        /// DELETE: api/users/{ip}
        /// </summary>
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
        /// Kullanıcı sayısını getirir
        /// GET: api/users/{ip}/count
        /// </summary>
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
