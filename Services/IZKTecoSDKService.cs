using System;
using System.Collections.Generic;
using ZKTecoApi.DTOs.Request;
using ZKTecoApi.DTOs.Response;

namespace ZKTecoApi.Services
{
    public interface IZKTecoSDKService
    {
        // Connection Management
        bool Connect(string ipAddress, int port);
        void Disconnect();
        bool IsConnected();

        // Device Information
        DeviceStatusResponse GetDeviceStatus(string ipAddress, int port);
        string GetSerialNumber();
        string GetFirmwareVersion();
        string GetPlatform();
        string GetDeviceModel();
        DateTime GetDeviceTime();
        bool SetDeviceTime(DateTime dateTime);

        // Capacity Information
        int GetUserCapacity();
        int GetLogCapacity();
        int GetUserCount();
        int GetLogCount();

        // User Management
        List<UserInfoResponse> GetAllUsers();
        UserInfoResponse GetUser(string enrollNumber);
        bool CreateUser(UserCreateRequest request);
        bool UpdateUser(string enrollNumber, UserUpdateRequest request);
        bool DeleteUser(string enrollNumber);
        bool ClearAllUsers();

        // Attendance Logs
        List<AttendanceLogResponse> GetAttendanceLogs(string deviceIp);
        bool ClearAttendanceLogs();

        // Device Control
        bool EnableDevice();
        bool DisableDevice();
        bool PowerOff();
        bool Restart();
        bool ClearAdministrators();

        // Realtime Events
        bool StartRealtimeEvents(string deviceIp, Action<RealtimeEventResponse> onEventReceived);
        bool StopRealtimeEvents();
    }
}
