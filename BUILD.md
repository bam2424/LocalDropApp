# Building LocalDropApp Standalone Executable

## Quick Build

### Windows (Recommended)
```bash
./build-exe.bat
```

### Manual Build
```bash
dotnet publish --framework net8.0-windows10.0.19041.0 --configuration Release --output "./publish" --self-contained true --runtime win-x64 /p:PublishSingleFile=true
```

## Output

- **Location**: `./publish/LocalDropApp.exe`
- **Size**: ~234MB (includes .NET runtime)
- **Requirements**: Windows 10+ (no additional dependencies needed)

## Distribution

The generated `LocalDropApp.exe` is completely self-contained and can be:
- ✅ Copied to any Windows computer and run directly
- ✅ Shared via USB, email, or file sharing
- ✅ Run multiple instances on the same machine for testing
- ✅ Distributed without requiring .NET installation

## Device Naming

When running multiple instances on the same computer:
- **First instance**: Uses the computer name (e.g., "DESKTOP-ABC123")
- **Additional instances**: Adds a number suffix (e.g., "DESKTOP-ABC1232", "DESKTOP-ABC1233")

## Network Requirements

- **Discovery**: UDP port 35731 (automatically finds available ports)
- **File Transfer**: TCP port 35732 (automatically finds available ports)
- **Firewall**: Windows may prompt to allow network access

## Testing Multiple Instances

```bash
# Copy to different folders
copy "./publish/LocalDropApp.exe" "C:/temp/instance1/LocalDropApp.exe"
copy "./publish/LocalDropApp.exe" "C:/temp/instance2/LocalDropApp.exe"

# Run from each folder
cd C:/temp/instance1 && LocalDropApp.exe
cd C:/temp/instance2 && LocalDropApp.exe
```

Each instance will automatically get a unique device name and network ports. 