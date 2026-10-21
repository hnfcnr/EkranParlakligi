@echo off
setlocal

echo ========================================================
echo   EkranParlakligi (ScreenBrightnessTray) Derleyici
echo ========================================================

set "CSC_PATH=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC_PATH%" (
    set "CSC_PATH=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if not exist "%CSC_PATH%" (
    echo [HATA] .NET Framework v4.0 csc.exe bulunamadi!
    pause
    exit /b 1
)

if not exist "%~dp0bin" mkdir "%~dp0bin"

echo [*] Derleniyor: src\BrightnessTray.cs -^> bin\BrightnessTray.exe...
"%CSC_PATH%" /target:winexe /optimize+ /out:"%~dp0bin\BrightnessTray.exe" "%~dp0src\BrightnessTray.cs"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [BASARILI] bin\BrightnessTray.exe basariyla derlendi.
    echo Boyut: ~16.5 KB - Sifir bagimlilik, yerel Win32 ve DDC-CI
) else (
    echo.
    echo [HATA] Derleme basarisiz oldu! Kod: %ERRORLEVEL%
)

endlocal
