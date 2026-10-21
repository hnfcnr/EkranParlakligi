# EkranParlakligi (Windows Screen Brightness Tray)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg)](https://microsoft.com/windows)
[![Binary Size](https://img.shields.io/badge/Binary%20Size-~16.5%20KB-success.svg)](bin/)
[![Dependencies](https://img.shields.io/badge/Dependencies-Zero%20(Built--in%20.NET)-brightgreen.svg)](src/)

Windows masaüstü ve harici monitörler için ultra hafif (~16.5 KB), sıfır bağımlılıklı, sistem tepsisi (system tray) tabanlı DDC/CI donanımsal parlaklık denetleyicisi.

An ultra-lightweight (~16.5 KB), zero-dependency, system tray-based DDC/CI hardware brightness controller for Windows external and multi-monitor setups.

---

## 🌟 Neden Bu Araç? / Why This Tool?

Windows işletim sisteminde harici masaüstü monitörler bağlandığında, yerel parlaklık ayar çubuğu genellikle **devre dışı (pasif)** kalır ve kullanıcının monitörün üzerindeki fiziksel tuşlarla uğraşmasını veya yüzlerce megabaytlık hantal üretici yazılımları kurmasını gerektirir.

**EkranParlakligi**, Windows'un yerel DDC/CI VESA standardı (`dxva2.dll`) ve Win32 API'lerini kullanarak:
1. Donanımsal parlaklığı doğrudan monitörün paneline iletir.
2. Tüm bağlı monitörleri tek hamlede senkronize eder.
3. Arka planda 10 MB'tan az RAM tüketir, sıfır CPU harcar.
4. Yalnızca 16.5 KB boyutundadır ve hiçbir harici kütüphane gerektirmez.

---

## ✨ Özellikler / Features

- 🖥️ **DDC/CI Donanım Desteği:** VESA DDC/CI protokolü ile monitörün dahili parlaklığını doğrudan değiştirir.
- 🎛️ **Modern Bildirim Alanı (System Tray) Arayüzü:** Tıklandığında Windows 11 tarzı açılır pencere (flyout) açılır.
- 🎚️ **Pürüzsüz Kaydırma Çubuğu:** 0 - 100 arasında hassas parlaklık ayarı.
- ⚡ **Hızlı Ön Ayar Butonları:** `%10`, `%25`, `%50`, `%75`, `%100` seviyelerine tek tıkla geçiş.
- 🌙 **Otomatik Gündüz / Akşam Zamanlayıcısı:**
  - `08:00 - 18:00`: Gündüz Modu (%100 parlaklık)
  - `18:00 - 08:00`: Akşam / Gece Modu (%10 parlaklık)
- 🔒 **Tekil Örnek (Single-Instance):** Çift tıklamalarda ikinci bir kopya açmaz, açık olan pencereyi öne getirir.
- 🛠️ **Sıfır Kurulum / Derleme Kolaylığı:** Windows ile birlikte gelen yerel `csc.exe` ile 1 saniyede derlenir.

---

## 🚀 Hızlı Başlangıç & Derleme / Quick Start & Build

Harici hiçbir SDK, Visual Studio veya paket yöneticisi kurmanıza gerek yoktur. Windows'ta yerleşik olarak bulunan .NET Framework 4.0 derleyicisi kullanılır.

### 1. Tek Tıkla Derleme (Build via Batch)
```cmd
build.bat
```
Çıktı `bin\BrightnessTray.exe` olarak üretilir (~16.5 KB).

### 2. Manuel Derleme (Manual via csc.exe)
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /optimize+ /out:bin\BrightnessTray.exe src\BrightnessTray.cs
```

### 3. Çalıştırma (Run)
`bin\BrightnessTray.exe` dosyasını çalıştırın. Sağ alt köşedeki sistem bildirim alanında (Tray) monitör simgesi belirecektir.

---

## 💻 PowerShell CLI Araçları / CLI Helpers

Arayüz olmaksızın komut satırından veya zamanlanmış görevlerden parlaklığı ayarlamak isterseniz `scripts/` klasöründeki betikleri kullanabilirsiniz:

```powershell
# Parlaklığı doğrudan %50 yap
powershell -ExecutionPolicy Bypass -File scripts\set_brightness.ps1 50

# Gündüz/Akşam kuralına göre otomatik parlaklık uygula (08:00-18:00 %100, diğer saatler %10)
powershell -ExecutionPolicy Bypass -File scripts\auto_brightness.ps1
```

---

## ⚙️ Başlangıçta Otomatik Çalıştırma / Run on Startup

Uygulamanın bilgisayarınız her açıldığında arka planda sessizce başlaması için:
1. `Win + R` tuşlarına basarak `shell:startup` yazın ve Enter'a basın.
2. `bin\BrightnessTray.exe` dosyasının kısayolunu bu klasöre yapıştırın.

---

## 🏗️ Mimari / Technical Architecture

```
EkranParlakligi/
├── bin/
│   └── BrightnessTray.exe        # ~16.5 KB derlenmiş yerel ikili
├── scripts/
│   ├── auto_brightness.ps1      # Zaman tabanlı CLI parlaklık betiği
│   └── set_brightness.ps1       # Doğrudan seviye belirleyen CLI betiği
├── src/
│   └── BrightnessTray.cs         # Win32 + DDC/CI C# kaynak kodu
├── build.bat                     # Windows csc.exe tek tıkla derleme betiği
├── LICENSE                       # MIT Lisansı
└── README.md
```

### Kullanılan Win32 & DDC/CI API'leri
- `user32.dll`: `EnumDisplayMonitors`, `SetForegroundWindow`, `RegisterWindowMessage`
- `dxva2.dll`: `GetNumberOfPhysicalMonitorsFromHMONITOR`, `GetPhysicalMonitorsFromHMONITOR`, `GetMonitorBrightness`, `SetMonitorBrightness`, `DestroyPhysicalMonitors`

---

## 📄 Lisans / License

Bu proje [MIT Lisansı](LICENSE) kapsamında açık kaynak olarak sunulmuştur.
