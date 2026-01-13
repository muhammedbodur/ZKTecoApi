using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using zkemkeeper;
using ZKTecoApi.DTOs.Request;
using ZKTecoApi.DTOs.Response;

namespace ZKTecoApi.Services
{
    public class ZKTecoSDKService : IZKTecoSDKService, IDisposable
    {
        private CZKEM _zkDevice;
        private bool _isConnected = false;
        private readonly int _machineNumber = 1;
        private readonly object _lockObject = new object();

        public ZKTecoSDKService()
        {
            _zkDevice = new CZKEMClass();
        }

        // ZKTeco SDK'sından gelen string'leri düzelt (encoding sorunu)
        private string FixEncoding(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            try
            {
                // ZKTeco SDK bazı durumlarda ISO-8859-9 (Turkish) encoding kullanıyor
                // Ama .NET bunu yanlış yorumluyor
                byte[] bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(input);
                return Encoding.GetEncoding("windows-1254").GetString(bytes);
            }
            catch
            {
                // Encoding dönüşümü başarısız olursa orijinal string'i döndür
                return input;
            }
        }

        #region Connection Management

        public bool Connect(string ipAddress, int port)
        {
            lock (_lockObject)
            {
                try
                {
                    if (_isConnected)
                    {
                        _zkDevice.Disconnect();
                    }

                    _isConnected = _zkDevice.Connect_Net(ipAddress, port);
                    return _isConnected;
                }
                catch (Exception)
                {
                    _isConnected = false;
                    return false;
                }
            }
        }

        public void Disconnect()
        {
            lock (_lockObject)
            {
                try
                {
                    if (_isConnected && _zkDevice != null)
                    {
                        _zkDevice.Disconnect();
                        _isConnected = false;
                    }
                }
                catch
                {
                    _isConnected = false;
                }
            }
        }

        public bool IsConnected()
        {
            return _isConnected;
        }

        #endregion

        #region Device Information

        public DeviceStatusResponse GetDeviceStatus(string ipAddress, int port)
        {
            lock (_lockObject)
            {
                try
                {
                    if (!Connect(ipAddress, port))
                    {
                        return null;
                    }

                    var response = new DeviceStatusResponse
                    {
                        IpAddress = ipAddress,
                        Port = port,
                        IsConnected = true,
                        SerialNumber = GetSerialNumber(),
                        FirmwareVersion = GetFirmwareVersion(),
                        Platform = GetPlatform(),
                        DeviceModel = GetDeviceModel(),
                        UserCount = GetUserCount(),
                        LogCount = GetLogCount(),
                        UserCapacity = GetUserCapacity(),
                        LogCapacity = GetLogCapacity(),
                        DeviceTime = GetDeviceTime(),
                        IsEnabled = true
                    };

                    Disconnect();
                    return response;
                }
                catch (Exception ex)
                {
                    Disconnect();
                    throw new Exception($"Device status error: {ex.Message}", ex);
                }
            }
        }

        public string GetSerialNumber()
        {
            string serialNumber = "";
            _zkDevice.GetSerialNumber(_machineNumber, out serialNumber);
            return serialNumber ?? "Unknown";
        }

        public string GetFirmwareVersion()
        {
            string firmwareVersion = "";
            _zkDevice.GetFirmwareVersion(_machineNumber, ref firmwareVersion);
            return firmwareVersion ?? "Unknown";
        }

        public string GetPlatform()
        {
            string platform = "";
            _zkDevice.GetPlatform(_machineNumber, ref platform);
            return platform ?? "Unknown";
        }

        public string GetDeviceModel()
        {
            // Platform bilgisi model olarak kullanılıyor
            return GetPlatform();
        }

        public DateTime GetDeviceTime()
        {
            int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
            _zkDevice.GetDeviceTime(_machineNumber, ref year, ref month, ref day, ref hour, ref minute, ref second);

            try
            {
                return new DateTime(year, month, day, hour, minute, second);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        public bool SetDeviceTime(DateTime dateTime)
        {
            return _zkDevice.SetDeviceTime2(_machineNumber,
                dateTime.Year, dateTime.Month, dateTime.Day,
                dateTime.Hour, dateTime.Minute, dateTime.Second);
        }

        #endregion

        #region Capacity Information

        public int GetUserCapacity()
        {
            int capacity = 0;
            _zkDevice.GetDeviceStatus(_machineNumber, 8, ref capacity);
            return capacity;
        }

        public int GetLogCapacity()
        {
            int capacity = 0;
            _zkDevice.GetDeviceStatus(_machineNumber, 9, ref capacity);
            return capacity;
        }

        public int GetUserCount()
        {
            int count = 0;
            _zkDevice.GetDeviceStatus(_machineNumber, 2, ref count);
            return count;
        }

        public int GetLogCount()
        {
            int count = 0;
            _zkDevice.GetDeviceStatus(_machineNumber, 6, ref count);
            return count;
        }

        #endregion

        #region User Management

        public List<UserInfoResponse> GetAllUsers()
        {
            var users = new List<UserInfoResponse>();

            try
            {
                _zkDevice.EnableDevice(_machineNumber, false);
                
                // ReadAllUserID - Kullanıcı okumaya hazırla
                if (!_zkDevice.ReadAllUserID(_machineNumber))
                {
                    _zkDevice.EnableDevice(_machineNumber, true);
                    return users;
                }

                // ZKTecoCtrl gibi: enrollNumber int ve ref kullan
                int enrollNumber = 0;
                string name = "";
                string password = "";
                int privilege = 0;
                bool enabled = false;

                int userCount = 0;
                // GetAllUserInfo metodu - ref kullanarak
                while (_zkDevice.GetAllUserInfo(_machineNumber, ref enrollNumber, ref name,
                    ref password, ref privilege, ref enabled))
                {
                    userCount++;

                    if (enrollNumber > 0)
                    {
                        // Her kullanıcı için kart numarasını al
                        // NOT: GetStrCardNumber SDK'da global bir buffer kullanır
                        // Bu yüzden her kullanıcı için tekrar GetUserInfo çağırıp sonra GetStrCardNumber çağırmalıyız
                        string userName = "";
                        string userPassword = "";
                        int userPrivilege = 0;
                        bool userEnabled = false;
                        
                        long? cardNumber = null;
                        
                        // Kullanıcı bilgilerini tekrar al (bu kart numarasını buffer'a yükler)
                        if (_zkDevice.GetUserInfo(_machineNumber, enrollNumber, ref userName, ref userPassword, ref userPrivilege, ref userEnabled))
                        {
                            string cardNumberStr = "";
                            if (_zkDevice.GetStrCardNumber(out cardNumberStr))
                            {
                                if (!string.IsNullOrEmpty(cardNumberStr) && long.TryParse(cardNumberStr, out long cardNum) && cardNum > 0)
                                {
                                    cardNumber = cardNum;
                                }
                            }
                        }

                        users.Add(new UserInfoResponse
                        {
                            EnrollNumber = enrollNumber.ToString(),
                            Name = name?.Trim() ?? "",
                            Password = password?.Trim() ?? "",
                            CardNumber = cardNumber,
                            Privilege = privilege,
                            Enabled = enabled
                        });
                    }

                    if (userCount > 10000) break; // Sonsuz döngü koruması
                }

                _zkDevice.EnableDevice(_machineNumber, true);
                return users;
            }
            catch (Exception ex)
            {
                _zkDevice.EnableDevice(_machineNumber, true);
                throw new Exception($"Get all users error: {ex.Message}", ex);
            }
        }

        public UserInfoResponse GetUser(string enrollNumber)
        {
            try
            {
                _zkDevice.EnableDevice(_machineNumber, false);

                if (!int.TryParse(enrollNumber, out int numericEnrollNumber))
                {
                    _zkDevice.EnableDevice(_machineNumber, true);
                    return null;
                }

                string name = "";
                string password = "";
                int privilege = 0;
                bool enabled = false;

                if (_zkDevice.GetUserInfo(_machineNumber, numericEnrollNumber, ref name,
                    ref password, ref privilege, ref enabled))
                {
                    string cardNumberStr = "";
                    _zkDevice.GetStrCardNumber(out cardNumberStr);
                    
                    long? cardNumber = null;
                    if (!string.IsNullOrEmpty(cardNumberStr) && long.TryParse(cardNumberStr, out long cardNum) && cardNum > 0)
                    {
                        cardNumber = cardNum;
                    }

                    _zkDevice.EnableDevice(_machineNumber, true);

                    return new UserInfoResponse
                    {
                        EnrollNumber = numericEnrollNumber.ToString(),
                        Name = name?.Trim() ?? "",
                        Password = password?.Trim() ?? "",
                        CardNumber = cardNumber,
                        Privilege = privilege,
                        Enabled = enabled
                    };
                }

                _zkDevice.EnableDevice(_machineNumber, true);
                return null;
            }
            catch (Exception ex)
            {
                _zkDevice.EnableDevice(_machineNumber, true);
                throw new Exception($"Get user error: {ex.Message}", ex);
            }
        }

        public bool CreateUser(UserCreateRequest request)
        {
            try
            {
                _zkDevice.EnableDevice(_machineNumber, false);

                if (!int.TryParse(request.EnrollNumber, out int numericEnrollNumber))
                {
                    _zkDevice.EnableDevice(_machineNumber, true);
                    return false;
                }

                // Kart numarası varsa önce buffer'a yükle
                if (request.CardNumber.HasValue && request.CardNumber.Value > 0)
                {
                    _zkDevice.SetStrCardNumber(request.CardNumber.Value.ToString());
                }

                // Sonra kullanıcıyı oluştur (kart numarası buffer'dan alınır)
                bool result = _zkDevice.SetUserInfo(_machineNumber,
                    numericEnrollNumber,
                    request.Name,
                    request.Password ?? "",
                    request.Privilege,
                    request.Enabled);

                _zkDevice.RefreshData(_machineNumber);
                _zkDevice.EnableDevice(_machineNumber, true);

                return result;
            }
            catch (Exception ex)
            {
                _zkDevice.EnableDevice(_machineNumber, true);
                throw new Exception($"Create user error: {ex.Message}", ex);
            }
        }

        public bool UpdateUser(string enrollNumber, UserUpdateRequest request)
        {
            try
            {
                _zkDevice.EnableDevice(_machineNumber, false);

                if (!int.TryParse(enrollNumber, out int numericEnrollNumber))
                {
                    _zkDevice.EnableDevice(_machineNumber, true);
                    return false;
                }

                // Önce mevcut kullanıcıyı al
                string existingName = "";
                string existingPassword = "";
                int existingPrivilege = 0;
                bool existingEnabled = false;

                if (!_zkDevice.GetUserInfo(_machineNumber, numericEnrollNumber, ref existingName,
                    ref existingPassword, ref existingPrivilege, ref existingEnabled))
                {
                    _zkDevice.EnableDevice(_machineNumber, true);
                    return false;
                }

                // Yeni değerleri uygula
                string newName = request.Name ?? existingName;
                string newPassword = request.Password ?? existingPassword;
                int newPrivilege = request.Privilege ?? existingPrivilege;
                bool newEnabled = request.Enabled ?? existingEnabled;

                // Kart numarası varsa önce buffer'a yükle
                if (request.CardNumber.HasValue)
                {
                    _zkDevice.SetStrCardNumber(request.CardNumber.Value.ToString());
                }

                // Sonra kullanıcıyı güncelle (kart numarası buffer'dan alınır)
                bool result = _zkDevice.SetUserInfo(_machineNumber,
                    numericEnrollNumber, newName, newPassword, newPrivilege, newEnabled);

                _zkDevice.RefreshData(_machineNumber);
                _zkDevice.EnableDevice(_machineNumber, true);

                return result;
            }
            catch (Exception ex)
            {
                _zkDevice.EnableDevice(_machineNumber, true);
                throw new Exception($"Update user error: {ex.Message}", ex);
            }
        }

        public bool DeleteUser(string enrollNumber)
        {
            try
            {
                _zkDevice.EnableDevice(_machineNumber, false);

                if (!int.TryParse(enrollNumber, out int numericEnrollNumber))
                {
                    _zkDevice.EnableDevice(_machineNumber, true);
                    return false;
                }

                // 12 = Tüm kullanıcı verilerini sil (parmak izi, kart, vb.)
                bool result = _zkDevice.SSR_DeleteEnrollData(_machineNumber, numericEnrollNumber.ToString(), 12);

                _zkDevice.RefreshData(_machineNumber);
                _zkDevice.EnableDevice(_machineNumber, true);

                return result;
            }
            catch (Exception ex)
            {
                _zkDevice.EnableDevice(_machineNumber, true);
                throw new Exception($"Delete user error: {ex.Message}", ex);
            }
        }

        public bool ClearAllUsers()
        {
            try
            {
                _zkDevice.EnableDevice(_machineNumber, false);

                // 5 = Tüm kullanıcı verilerini temizle
                bool result = _zkDevice.ClearData(_machineNumber, 5);

                _zkDevice.RefreshData(_machineNumber);
                _zkDevice.EnableDevice(_machineNumber, true);

                return result;
            }
            catch (Exception ex)
            {
                _zkDevice.EnableDevice(_machineNumber, true);
                throw new Exception($"Clear all users error: {ex.Message}", ex);
            }
        }

        #endregion

        #region Attendance Logs

        public List<AttendanceLogResponse> GetAttendanceLogs(string deviceIp)
        {
            var logs = new List<AttendanceLogResponse>();

            try
            {
                _zkDevice.EnableDevice(_machineNumber, false);
                
                if (!_zkDevice.ReadGeneralLogData(_machineNumber))
                {
                    _zkDevice.EnableDevice(_machineNumber, true);
                    return logs;
                }

                // ZKTecoCtrl gibi: enrollNumber int ve ref kullan
                int enrollNumber = 0;
                int verifyMode = 0;
                int inOutMode = 0;
                int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
                int workCode = 0;
                int reserved = 0;

                // GetGeneralExtLogData kullan - SSR değil!
                while (_zkDevice.GetGeneralExtLogData(_machineNumber, ref enrollNumber, ref verifyMode, 
                    ref inOutMode, ref year, ref month, ref day, ref hour, ref minute, ref second, 
                    ref workCode, ref reserved))
                {
                    try
                    {
                        // Geçerli tarih kontrolü
                        if (year < 2000 || year > 2100 || month < 1 || month > 12 || day < 1 || day > 31)
                            continue;

                        var dateTime = new DateTime(year, month, day, hour, minute, second);

                        logs.Add(new AttendanceLogResponse
                        {
                            EnrollNumber = enrollNumber > 0 ? enrollNumber.ToString() : "",
                            DateTime = dateTime,
                            VerifyMethod = verifyMode,
                            InOutMode = inOutMode,
                            WorkCode = workCode,
                            DeviceIp = deviceIp
                        });
                    }
                    catch
                    {
                        // Geçersiz tarih verisi, atla
                        continue;
                    }
                }

                _zkDevice.EnableDevice(_machineNumber, true);
                return logs;
            }
            catch (Exception ex)
            {
                _zkDevice.EnableDevice(_machineNumber, true);
                throw new Exception($"Get attendance logs error: {ex.Message}", ex);
            }
        }

        public bool ClearAttendanceLogs()
        {
            try
            {
                _zkDevice.EnableDevice(_machineNumber, false);

                bool result = _zkDevice.ClearGLog(_machineNumber);

                _zkDevice.RefreshData(_machineNumber);
                _zkDevice.EnableDevice(_machineNumber, true);

                return result;
            }
            catch (Exception ex)
            {
                _zkDevice.EnableDevice(_machineNumber, true);
                throw new Exception($"Clear attendance logs error: {ex.Message}", ex);
            }
        }

        #endregion

        #region Device Control

        public bool EnableDevice()
        {
            return _zkDevice.EnableDevice(_machineNumber, true);
        }

        public bool DisableDevice()
        {
            return _zkDevice.EnableDevice(_machineNumber, false);
        }

        public bool PowerOff()
        {
            return _zkDevice.PowerOffDevice(_machineNumber);
        }

        public bool Restart()
        {
            return _zkDevice.RestartDevice(_machineNumber);
        }

        public bool ClearAdministrators()
        {
            return _zkDevice.ClearAdministrators(_machineNumber);
        }

        #endregion

        #region Realtime Events

        public bool StartRealtimeEvents(string deviceIp, Action<RealtimeEventResponse> onEventReceived)
        {
            try
            {
                _zkDevice.OnAttTransactionEx += new _IZKEMEvents_OnAttTransactionExEventHandler(
                    (string enrollNumber, int isInValid, int attState, int verifyMethod,
                     int year, int month, int day, int hour, int minute, int second, int workCode) =>
                    {
                        try
                        {
                            var evt = new RealtimeEventResponse
                            {
                                EventType = "Attendance",
                                EnrollNumber = enrollNumber,
                                EventTime = new DateTime(year, month, day, hour, minute, second),
                                VerifyMethod = verifyMethod,
                                InOutMode = attState,
                                WorkCode = workCode,
                                DeviceIp = deviceIp,
                                IsValid = isInValid == 0 // 0 = valid, 1 = invalid
                            };

                            onEventReceived?.Invoke(evt);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Realtime event error: {ex.Message}");
                        }
                    });

                // 65535 = Tüm event'leri dinle
                return _zkDevice.RegEvent(_machineNumber, 65535);
            }
            catch (Exception ex)
            {
                throw new Exception($"Start realtime events error: {ex.Message}", ex);
            }
        }

        public bool StopRealtimeEvents()
        {
            try
            {
                // Event listener'ı kaldır
                _zkDevice.OnAttTransactionEx -= null;
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                Disconnect();

                if (_zkDevice != null)
                {
                    Marshal.ReleaseComObject(_zkDevice);
                    _zkDevice = null;
                }
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        #endregion
    }
}
