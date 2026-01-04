# ZKTecoApi

ZKTeco cihazlarını yönetmek için stateless REST API proxy'si. Bu API, ZKTeco SDK'sını HTTP endpoint'leri üzerinden kullanılabilir hale getirir ve SignalR ile gerçek zamanlı event desteği sağlar.

## Özellikler

- **Stateless Architecture**: Her istek bağımsızdır, connection pooling yok
- **REST API**: Tüm ZKTeco operasyonları HTTP üzerinden erişilebilir
- **Realtime Events**: SignalR ile cihazdan gelen event'leri gerçek zamanlı dinleme
- **Kapsamlı Endpoints**: Kullanıcı yönetimi, yoklama kayıtları, cihaz kontrolü
- **Kart Numarası Desteği**: Kullanıcıları kart numarasıyla sorgulama

## Kurulum

### Gereksinimler

- .NET Framework 4.8
- Visual Studio 2019 veya üzeri
- ZKTeco Standalone SDK (zkemkeeper.dll)

### Adımlar

1. Projeyi klonlayın:
```bash
git clone https://github.com/muhammedbodur/ZKTecoApi.git
cd ZKTecoApi
```

2. NuGet paketlerini yükleyin:
```bash
nuget restore
```

3. ZKTeco SDK'sını projeye ekleyin:
   - `zkemkeeper.dll` dosyasını projeye referans olarak ekleyin
   - COM Interop için gerekli konfigürasyonları yapın

4. Projeyi derleyin ve çalıştırın

## API Endpoints

### Health Check

```http
GET /api/health
```

API'nin sağlık durumunu kontrol eder.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-04T10:30:00"
}
```

---

### Device Endpoints

#### Get Device Status
```http
GET /api/device/{ip}/status?port=4370
```

Cihazın detaylı durum bilgilerini getirir.

**Parameters:**
- `ip` (required): Cihaz IP adresi
- `port` (optional, default: 4370): Cihaz port numarası

**Response:**
```json
{
  "success": true,
  "data": {
    "ipAddress": "192.168.1.201",
    "port": 4370,
    "isConnected": true,
    "platform": "ZEM800",
    "firmwareVersion": "Ver 6.60",
    "serialNumber": "BBJA204160001",
    "deviceModel": "F18",
    "userCount": 25,
    "logCount": 1500,
    "userCapacity": 3000,
    "logCapacity": 100000,
    "deviceTime": "2026-01-04T10:30:00",
    "isEnabled": true
  }
}
```

#### Get/Set Device Time
```http
GET /api/device/{ip}/time?port=4370
POST /api/device/{ip}/time?port=4370
```

**POST Body:**
```json
"2026-01-04T10:30:00"
```

#### Device Control
```http
POST /api/device/{ip}/enable?port=4370
POST /api/device/{ip}/disable?port=4370
POST /api/device/{ip}/restart?port=4370
POST /api/device/{ip}/poweroff?port=4370
```

---

### User Endpoints

#### Get All Users
```http
GET /api/users/{ip}?port=4370
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "enrollNumber": "1001",
      "name": "Ahmet Yılmaz",
      "password": "",
      "cardNumber": 123456789,
      "privilege": 0,
      "enabled": true
    }
  ],
  "count": 1
}
```

#### Get User by Enroll Number
```http
GET /api/users/{ip}/{enrollNumber}?port=4370
```

#### Get User by Card Number ⭐
```http
GET /api/users/{ip}/card/{cardNumber}?port=4370
```

**Example:**
```http
GET /api/users/192.168.1.201/card/123456789?port=4370
```

**Response:**
```json
{
  "success": true,
  "data": {
    "enrollNumber": "1001",
    "name": "Ahmet Yılmaz",
    "password": "",
    "cardNumber": 123456789,
    "privilege": 0,
    "enabled": true
  }
}
```

#### Create User
```http
POST /api/users/{ip}?port=4370
Content-Type: application/json

{
  "enrollNumber": "1001",
  "name": "Ahmet Yılmaz",
  "password": "1234",
  "cardNumber": 123456789,
  "privilege": 0,
  "enabled": true
}
```

**Privilege Levels:**
- `0`: User
- `1`: Enroller
- `2`: Manager
- `3`: Super Admin

#### Update User
```http
PUT /api/users/{ip}/{enrollNumber}?port=4370
Content-Type: application/json

{
  "name": "Ahmet Yılmaz (Updated)",
  "password": "5678",
  "cardNumber": 987654321,
  "privilege": 2,
  "enabled": true
}
```

#### Delete User
```http
DELETE /api/users/{ip}/{enrollNumber}?port=4370
```

#### Clear All Users
```http
DELETE /api/users/{ip}?port=4370
```

#### Get User Count
```http
GET /api/users/{ip}/count?port=4370
```

---

### Attendance Endpoints

#### Get Attendance Logs
```http
GET /api/attendance/{ip}?port=4370
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "enrollNumber": "1001",
      "dateTime": "2026-01-04T08:30:00",
      "verifyMethod": 1,
      "inOutMode": 0,
      "workCode": 0,
      "deviceIp": "192.168.1.201"
    }
  ],
  "count": 1
}
```

**Verify Method:**
- `0`: Password
- `1`: Fingerprint
- `15`: Card
- `25`: Face

**InOut Mode (AttendanceState):**
- `0`: Check In
- `1`: Check Out
- `2`: Break Out
- `3`: Break In
- `4`: OT In
- `5`: OT Out

#### Clear Attendance Logs
```http
DELETE /api/attendance/{ip}?port=4370
```

#### Get Log Count
```http
GET /api/attendance/{ip}/count?port=4370
```

---

### Realtime Events

#### Start Realtime Monitoring
```http
POST /api/realtime/{ip}/start?port=4370
```

Bu endpoint'i çağırdıktan sonra, SignalR üzerinden gerçek zamanlı event'leri dinleyebilirsiniz.

#### Stop Realtime Monitoring
```http
POST /api/realtime/{ip}/stop?port=4370
```

---

## SignalR Realtime Events

### JavaScript Client Örneği

```html
<!DOCTYPE html>
<html>
<head>
    <script src="https://code.jquery.com/jquery-3.6.0.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
</head>
<body>
    <script>
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:PORT/signalr")
            .build();

        // Event dinleyicileri
        connection.on("onRealtimeEvent", function (event) {
            console.log("Realtime Event:", event);
            // event.enrollNumber
            // event.eventTime
            // event.verifyMethod
            // event.inOutMode
            // event.deviceIp
        });

        connection.on("onSubscribed", function (data) {
            console.log("Subscribed:", data);
        });

        connection.on("onUnsubscribed", function (data) {
            console.log("Unsubscribed:", data);
        });

        // Bağlan
        connection.start().then(function () {
            console.log("Connected to SignalR");

            // Belirli bir cihaza abone ol
            connection.invoke("SubscribeToDevice", "192.168.1.201");
        }).catch(function (err) {
            console.error(err.toString());
        });

        // Abonelikten çık
        function unsubscribe() {
            connection.invoke("UnsubscribeFromDevice", "192.168.1.201");
        }
    </script>
</body>
</html>
```

### C# Client Örneği

```csharp
using Microsoft.AspNet.SignalR.Client;

var hubConnection = new HubConnection("http://localhost:PORT/signalr");
var hubProxy = hubConnection.CreateHubProxy("RealtimeEventHub");

// Event handler
hubProxy.On<RealtimeEventResponse>("onRealtimeEvent", (evt) =>
{
    Console.WriteLine($"Event: {evt.EnrollNumber} at {evt.EventTime}");
});

// Bağlan
await hubConnection.Start();

// Cihaza abone ol
await hubProxy.Invoke("SubscribeToDevice", "192.168.1.201");
```

---

## Event Flow

1. **Start Realtime Monitoring**: `POST /api/realtime/{ip}/start`
2. **Connect to SignalR Hub**: Client SignalR'a bağlanır
3. **Subscribe to Device**: `SubscribeToDevice(deviceIp)` metodunu çağır
4. **Receive Events**: `onRealtimeEvent` handler'ı ile event'leri al
5. **Unsubscribe**: İsteğe bağlı olarak `UnsubscribeFromDevice(deviceIp)` çağır
6. **Stop Monitoring**: `POST /api/realtime/{ip}/stop`

---

## Mimari

```
┌─────────────────┐
│  Client App     │
│  (SGKPortalApp) │
└────────┬────────┘
         │
    HTTP │ REST API
         │
┌────────▼────────┐
│   ZKTecoApi     │
│   Controllers   │
└────────┬────────┘
         │
┌────────▼────────┐
│  SDK Service    │
│  (Stateless)    │
└────────┬────────┘
         │
    SDK  │ zkemkeeper.dll
         │
┌────────▼────────┐
│  ZKTeco Device  │
│  (192.168.x.x)  │
└─────────────────┘
```

---

## AttendanceState (InOutMode) Açıklaması

ZKTeco cihazlarından gelen `InOutMode` değeri, yoklama durumunu belirtir:

- **0 (CheckIn)**: Giriş - Çalışan işyerine geldiğinde
- **1 (CheckOut)**: Çıkış - Çalışan işyerinden ayrıldığında
- **2 (BreakOut)**: Mola Başlangıcı - Çalışan molaya çıktığında
- **3 (BreakIn)**: Mola Bitişi - Çalışan moladan döndüğünde
- **4 (OTIn)**: Fazla Mesai Başlangıcı
- **5 (OTOut)**: Fazla Mesai Bitişi

Bu değerler cihaz ayarlarına göre yapılandırılır ve her yoklama kaydında `InOutMode` field'ında gelir. Cihazda bu modlar tanımlanmamışsa, genellikle sadece 0 (Giriş) ve 1 (Çıkış) değerleri kullanılır.

---

## Konfigürasyon

### Web.config AppSettings

```xml
<appSettings>
  <add key="DefaultDevicePort" value="4370" />
  <add key="ConnectionTimeout" value="5000" />
</appSettings>
```

---

## Lisans

Bu proje Apache 2.0 lisansı altında lisanslanmıştır.

---

## Referans Projeler

- **ZKTecoCtrl**: https://github.com/muhammedbodur/ZKTecoCtrl
- **SGKPortalApp**: https://github.com/muhammedbodur/SGKPortalApp

---

## Notlar

- Her API çağrısı bağımsızdır (stateless)
- SDK entegrasyonu için `zkemkeeper.dll` gereklidir (TODO comment'lerde belirtilmiştir)
- Realtime event'ler için önce `/api/realtime/{ip}/start` endpoint'ini çağırmalısınız
- Kart numarası ile kullanıcı sorgulamak için `/api/users/{ip}/card/{cardNumber}` endpoint'ini kullanın

---

## Geliştirme Durumu

✅ **ZKTeco SDK entegrasyonu tamamlandı**

- zkemkeeper COM referansı projeye eklendi
- Tüm SDK operasyonları `ZKTecoSDKService.cs` içinde implement edildi
- Stateless yapıya uygun, her istek bağımsız çalışır
- Production kullanıma hazır

**NOT**: zkemkeeper COM component'inin sistemde kayıtlı olması gerekir. ZKTeco SDK kurulumundan sonra otomatik olarak kayıt edilir.
