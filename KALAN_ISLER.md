# 📋 EkranParlakligi - Kalan İşler & Yol Haritası (Backlog & Roadmap)

Windows harici monitörler için DDC/CI donanımsal parlaklık denetleyicisi.

---

## 🎯 Tamamlanan Özellikler (v1.0.0)
- [x] **DDC/CI Donanım Entegrasyonu:** `dxva2.dll` ile harici monitörlerin donanımsal parlaklığını okuma ve yazma.
- [x] **Çift Monitör Senkronizasyonu:** Tüm bağlı monitörleri tek hamlede senkronize etme.
- [x] **Bildirim Alanı (System Tray) Uygulaması:** Modern Windows flyout popup arayüzü.
- [x] **Hassas Kaydırma Çubuğu:** 0-100% arası yumuşak trackbar kontrolü.
- [x] **Hızlı Ön Ayar Butonları:** `%10`, `%25`, `%50`, `%75`, `%100` butonları.
- [x] **Otomatik Gündüz / Akşam Kuralı:** 08:00 - 18:00 arası %100, 18:00 - 08:00 arası %10 otomatik geçiş.
- [x] **Tekil Örnek Koruması (Single-Instance):** Adlandırılmış Mutex ve pencere öne getirme aktivasyonu.
- [x] **PowerShell CLI Betikleri:** `scripts/set_brightness.ps1` ve `scripts/auto_brightness.ps1`.
- [x] **Sıfır Bağımlılıklı Derleme:** Windows dahili `csc.exe` ile 16.5 KB derleme altyapısı (`build.bat`).

---

## 🔮 Gelecek Planlar (Roadmap)
- [ ] Monitör bazında bağımsız parlaklık ayarı (Monitör 1 / Monitör 2 ayrı kontrol imkanı).
- [ ] Ayarlar menüsü üzerinden gündüz/gece saatlerini dinamik olarak özelleştirme.
- [ ] Gün doğumu / gün batımı saatlerine göre konumsal otomatik parlaklık geçişi.
- [ ] Kontrast ayarı desteği (DDC/CI contrast control).
