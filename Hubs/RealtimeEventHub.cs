using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using ZKTecoApi.DTOs.Response;

namespace ZKTecoApi.Hubs
{
    public class RealtimeEventHub : Hub
    {
        private static readonly Dictionary<string, HashSet<string>> DeviceSubscriptions = new Dictionary<string, HashSet<string>>();
        private static readonly object _lock = new object();

        public override Task OnConnected()
        {
            var connectionId = Context.ConnectionId;
            Console.WriteLine($"Client connected: {connectionId}");
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var connectionId = Context.ConnectionId;
            Console.WriteLine($"Client disconnected: {connectionId}");

            // Bağlantı koptuğunda, bu connection'ın tüm aboneliklerini temizle
            lock (_lock)
            {
                var devicesToRemove = new List<string>();
                foreach (var kvp in DeviceSubscriptions)
                {
                    kvp.Value.Remove(connectionId);
                    if (kvp.Value.Count == 0)
                    {
                        devicesToRemove.Add(kvp.Key);
                    }
                }

                foreach (var device in devicesToRemove)
                {
                    DeviceSubscriptions.Remove(device);
                }
            }

            return base.OnDisconnected(stopCalled);
        }

        /// <summary>
        /// Belirli bir cihazın event'lerine abone ol
        /// </summary>
        public void SubscribeToDevice(string deviceIp)
        {
            var connectionId = Context.ConnectionId;
            lock (_lock)
            {
                if (!DeviceSubscriptions.ContainsKey(deviceIp))
                {
                    DeviceSubscriptions[deviceIp] = new HashSet<string>();
                }
                DeviceSubscriptions[deviceIp].Add(connectionId);
            }

            Clients.Caller.onSubscribed(new { deviceIp, message = $"Subscribed to {deviceIp}" });
            Console.WriteLine($"Client {connectionId} subscribed to device {deviceIp}");
        }

        /// <summary>
        /// Belirli bir cihazın event'lerinden aboneliği iptal et
        /// </summary>
        public void UnsubscribeFromDevice(string deviceIp)
        {
            var connectionId = Context.ConnectionId;
            lock (_lock)
            {
                if (DeviceSubscriptions.ContainsKey(deviceIp))
                {
                    DeviceSubscriptions[deviceIp].Remove(connectionId);
                    if (DeviceSubscriptions[deviceIp].Count == 0)
                    {
                        DeviceSubscriptions.Remove(deviceIp);
                    }
                }
            }

            Clients.Caller.onUnsubscribed(new { deviceIp, message = $"Unsubscribed from {deviceIp}" });
            Console.WriteLine($"Client {connectionId} unsubscribed from device {deviceIp}");
        }

        /// <summary>
        /// Realtime event'i tüm ilgili client'lara broadcast et
        /// </summary>
        public static void BroadcastRealtimeEvent(RealtimeEventResponse eventData)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<RealtimeEventHub>();
            var deviceIp = eventData.DeviceIp;

            lock (_lock)
            {
                if (DeviceSubscriptions.ContainsKey(deviceIp))
                {
                    foreach (var connectionId in DeviceSubscriptions[deviceIp])
                    {
                        context.Clients.Client(connectionId).onRealtimeEvent(eventData);
                    }
                    Console.WriteLine($"Event broadcasted to {DeviceSubscriptions[deviceIp].Count} clients for device {deviceIp}");
                }
            }
        }
    }
}
