@echo off
echo Building LocalDropApp standalone executable...
echo.

REM Clean previous publish
if exist "publish" rmdir /s /q "publish"

REM Build the standalone executable
dotnet publish --framework net8.0-windows10.0.19041.0 --configuration Release --output "./publish" --self-contained true --runtime win-x64 /p:PublishSingleFile=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Build successful!
    echo 📁 Executable created: .\publish\LocalDropApp.exe
    echo 📊 File size: 
    dir "publish\LocalDropApp.exe" | find "LocalDropApp.exe"
    echo.
    echo Ready to distribute! 🚀
) else (
    echo.
    echo ❌ Build failed!
    exit /b 1
)

pause 