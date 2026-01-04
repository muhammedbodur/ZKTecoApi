using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
            string model = "";
            _zkDevice.GetDeviceInfo(_machineNumber, 1, ref model);
            return model ?? "Unknown";
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
            string capacity = "";
            _zkDevice.GetDeviceInfo(_machineNumber, 4, ref capacity);
            return int.TryParse(capacity, out int result) ? result : 0;
        }

        public int GetLogCapacity()
        {
            string capacity = "";
            _zkDevice.GetDeviceInfo(_machineNumber, 5, ref capacity);
            return int.TryParse(capacity, out int result) ? result : 0;
        }

        public int GetUserCount()
        {
            string count = "";
            _zkDevice.GetDeviceInfo(_machineNumber, 2, ref count);
            return int.TryParse(count, out int result) ? result : 0;
        }

        public int GetLogCount()
        {
            string count = "";
            _zkDevice.GetDeviceInfo(_machineNumber, 8, ref count);
            return int.TryParse(count, out int result) ? result : 0;
        }

        #endregion

        #region User Management

        public List<UserInfoResponse> GetAllUsers()
        {
            var users = new List<UserInfoResponse>();

            try
            {
                _zkDevice.EnableDevice(_machineNumber, false);
                _zkDevice.ReadAllUserID(_machineNumber);

                string enrollNumber = "";
                string name = "";
                string password = "";
                int privilege = 0;
                bool enabled = true;

                while (_zkDevice.SSR_GetAllUserInfo(_machineNumber, out enrollNumber, out name,
                    out password, out privilege, out enabled))
                {
                    long cardNumber = 0;
                    _zkDevice.GetStrCardNumber(out string cardNumberStr);
                    long.TryParse(cardNumberStr, out cardNumber);

                    users.Add(new UserInfoResponse
                    {
                        EnrollNumber = enrollNumber,
                        Name = name,
                        Password = password,
                        CardNumber = cardNumber > 0 ? cardNumber : (long?)null,
                        Privilege = privilege,
                        Enabled = enabled
                    });
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

                string name = "";
                string password = "";
                int privilege = 0;
                bool enabled = true;

                if (_zkDevice.SSR_GetUserInfo(_machineNumber, enrollNumber, out name,
                    out password, out privilege, out enabled))
                {
                    long cardNumber = 0;
                    _zkDevice.GetStrCardNumber(out string cardNumberStr);
                    long.TryParse(cardNumberStr, out cardNumber);

                    _zkDevice.EnableDevice(_machineNumber, true);

                    return new UserInfoResponse
                    {
                        EnrollNumber = enrollNumber,
                        Name = name,
                        Password = password,
                        CardNumber = cardNumber > 0 ? cardNumber : (long?)null,
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

                bool result = _zkDevice.SSR_SetUserInfo(_machineNumber,
                    request.EnrollNumber,
                    request.Name,
                    request.Password ?? "",
                    request.Privilege,
                    request.Enabled);

                if (result && request.CardNumber.HasValue)
                {
                    _zkDevice.SetStrCardNumber(request.CardNumber.Value.ToString());
                }

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

                // Önce mevcut kullanıcıyı al
                string existingName = "";
                string existingPassword = "";
                int existingPrivilege = 0;
                bool existingEnabled = true;

                if (!_zkDevice.SSR_GetUserInfo(_machineNumber, enrollNumber, out existingName,
                    out existingPassword, out existingPrivilege, out existingEnabled))
                {
                    _zkDevice.EnableDevice(_machineNumber, true);
                    return false;
                }

                // Yeni değerleri uygula
                string newName = request.Name ?? existingName;
                string newPassword = request.Password ?? existingPassword;
                int newPrivilege = request.Privilege ?? existingPrivilege;
                bool newEnabled = request.Enabled ?? existingEnabled;

                bool result = _zkDevice.SSR_SetUserInfo(_machineNumber,
                    enrollNumber, newName, newPassword, newPrivilege, newEnabled);

                if (result && request.CardNumber.HasValue)
                {
                    _zkDevice.SetStrCardNumber(request.CardNumber.Value.ToString());
                }

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

                // 12 = Tüm kullanıcı verilerini sil (parmak izi, kart, vb.)
                bool result = _zkDevice.SSR_DeleteEnrollData(_machineNumber, enrollNumber, 12);

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
                _zkDevice.ReadGeneralLogData(_machineNumber);

                string enrollNumber = "";
                int verifyMethod = 0;
                int inOutMode = 0;
                int year = 0, month = 0, day = 0, hour = 0, minute = 0, second = 0;
                int workCode = 0;

                while (_zkDevice.SSR_GetGeneralLogData(_machineNumber, out enrollNumber,
                    out verifyMethod, out inOutMode, out year, out month, out day,
                    out hour, out minute, out second, ref workCode))
                {
                    try
                    {
                        logs.Add(new AttendanceLogResponse
                        {
                            EnrollNumber = enrollNumber,
                            DateTime = new DateTime(year, month, day, hour, minute, second),
                            VerifyMethod = verifyMethod,
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
                    (string enrollNumber, int attState, int verifyMethod, int year, int month,
                     int day, int hour, int minute, int second, int workCode) =>
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
                                DeviceIp = deviceIp
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
