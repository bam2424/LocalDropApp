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

### **Running the Application**

1. **Clone & Open**
   ```bash
   git clone <repository-url>
   cd LocalDropApp
   ```

2. **Open in Visual Studio 2022**
   - Open `LocalDropApp.sln`
   - Select target platform (Windows Machine, Android, iOS)

3. **Build & Run**
   - Press `F5` or click "Start Debugging"
   - The app will launch with mock data to demonstrate UI functionality

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
```

## 🗺️ Roadmap

### **Phase 2: Networking Core** (Next)
- [ ] UDP broadcast peer discovery
- [ ] TCP file transfer implementation
- [ ] Real file picker integration
- [ ] Network security and validation

### **Phase 3: Integration & Polish**
- [ ] UI-backend integration
- [ ] Cross-platform testing
- [ ] Error handling and robustness
- [ ] Performance optimization

### **Phase 4: Advanced Features**
- [ ] Bonjour/Zeroconf discovery
- [ ] Transfer encryption
- [ ] Folder transfer support
- [ ] Remote file browsing

## 🎨 UI Showcase

The application features a modern, intuitive interface with:

- **📡 Header Section**: Device info and refresh controls
- **🌐 Peer Discovery**: Interactive peer list with status indicators
- **📁 File Selection**: Drag-drop area with file management
- **📊 Transfer Status**: Real-time progress monitoring
- **📋 Transfer History**: Complete activity log

## 🤝 Contributing

This project follows a phased development approach:

1. **Phase 1**: ✅ Complete UI implementation (DONE)
2. **Phase 2**: Core networking functionality
3. **Phase 3**: Integration and polish
4. **Phase 4**: Advanced features

Each phase is broken down into manageable tasks (see `TASKS.md` for details).

## 📄 License

This project is open source and available under the [MIT License](LICENSE).

## 🎯 Next Steps

The UI foundation is complete! The next major milestone is implementing the networking layer to enable real peer discovery and file transfers. The beautiful interface is ready to be connected to the UDP/TCP backend services.

**Ready to run and demo!** 🚀 