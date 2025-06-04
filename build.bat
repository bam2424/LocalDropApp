@echo off
echo Building LocalDropApp standalone executable...
echo.

REM Clean previous builds
if exist "release" rmdir /s /q "release"

echo Cleaning and building...
dotnet clean -c Release
dotnet publish --framework net8.0-windows10.0.19041.0 ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "./release" ^
    /p:PublishSingleFile=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:PublishTrimmed=false

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Build successful!
    echo 📁 Ready to distribute: .\release\LocalDropApp.exe
    dir "release\LocalDropApp.exe" | find "LocalDropApp.exe"
    echo.
    echo Ready for GitHub release! 🚀
) else (
    echo ❌ Build failed!
    exit /b 1
)

pause 