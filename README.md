# 📡 Local Drop - AirDrop Clone

A cross-platform file sharing application built with .NET MAUI that enables seamless file transfers between devices on the same local network.

## 🎯 Project Overview

Local Drop is a modern, intuitive file sharing application that mimics AirDrop functionality for Windows, Android, and iOS devices. It uses UDP for peer discovery and TCP for reliable file transfers, all wrapped in a beautiful, responsive user interface.

## ✨ Features

### 🎨 **Phase 1: Complete UI Implementation** ✅
- **Modern Interface**: Clean, card-based design with professional color scheme
- **Peer Discovery**: Real-time peer list with online/offline status indicators
- **File Selection**: Drag-and-drop area with multi-file selection support
- **Transfer Monitoring**: Live progress bars with speed and time estimates
- **Transfer History**: Complete log of all file transfer activities
- **Responsive Design**: Optimized for different screen sizes and orientations

### 🔧 **Phase 2: Core Networking** (Coming Next)
- **UDP Peer Discovery**: Automatic device discovery on local network
- **TCP File Transfer**: Reliable, chunked file streaming
- **Progress Tracking**: Real-time transfer progress and speed monitoring
- **Error Handling**: Robust connection management and recovery

## 🏗️ Architecture

### **MVVM Pattern**
- **Models**: `PeerDevice`, `FileTransfer` with INotifyPropertyChanged
- **ViewModels**: `MainViewModel` with CommunityToolkit.Mvvm
- **Views**: Modern XAML with data binding and converters

### **Project Structure**
```
LocalDropApp/
├── Models/              # Data models
├── ViewModels/          # MVVM view models  
├── Views/               # XAML pages
├── Services/            # Core services (coming in Phase 2)
├── Converters/          # Value converters for UI binding
└── Resources/           # Styles, colors, and assets
```

## 🚀 Getting Started

### **Prerequisites**
- Visual Studio 2022 (17.8 or later)
- .NET 8.0 SDK
- Windows 10/11 for Windows development
- Xcode for iOS development (macOS)
- Android SDK for Android development

### **Building Standalone Executable**

1. **Download and Extract**
   - Download the source code
   - Extract to a folder

2. **Build Distribution Version**
   - Double-click `BuildLocalDropApp.exe`
   - Wait for the build to complete
   - Find the ready-to-distribute executable in `./release/LocalDropApp.exe`

3. **Development in Visual Studio**
   - Open `LocalDropApp.sln` in Visual Studio 2022
   - Select target platform (Windows Machine, Android, iOS)
   - Press `F5` to run with mock data

### **Available Commands**
```bash
# Build the project
dotnet build

# Run on specific platform
dotnet run --framework net8.0-windows10.0.19041.0
```

## 🎮 Demo Features

The current UI implementation includes:

### **📱 Interactive Elements**
- **Discover Button**: Simulates finding new peers on the network
- **Select Files**: Mock file picker with pre-selected demo files
- **Send Files**: Initiates simulated file transfer with progress bars
- **Clear History**: Removes completed transfers from history
- **Refresh Peers**: Re-scans for available devices

### **📊 Real-time Updates**
- Live progress bars during transfers
- Status indicators (Online/Offline) for peers
- Transfer speed and time remaining estimates
- Visual feedback for all user interactions

## 🛠️ Technical Details

### **Dependencies**
- **.NET MAUI**: Cross-platform UI framework
- **CommunityToolkit.Mvvm**: MVVM helpers and source generators
- **CommunityToolkit.Maui**: Enhanced UI controls and behaviors

### **Key Components**

**PeerDevice Model**
```csharp
public class PeerDevice : INotifyPropertyChanged
{
    public string Name { get; set; }
    public string IpAddress { get; set; }
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; }
}
```

**FileTransfer Model**
```csharp
public class FileTransfer : INotifyPropertyChanged
{
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public TransferStatus Status { get; set; }
    public double ProgressPercentage { get; set; }
}
