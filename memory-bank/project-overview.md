# MapleViewCapture Project Overview

## Project Description
A real-time screen capture and template matching application specifically designed for MapleStory Land gameplay automation and monitoring.

## Current Project Status: **WELL-DEVELOPED - Core Features Complete**

### Architecture & Components

#### 1. Core Capture System ✅ COMPLETE
- **MainForm.cs**: Primary UI and orchestration layer
- **ScreenCapture.cs**: Window capture functionality using Graphics.CopyFromScreen
- **DirectXCapture.cs**: Alternative capture method (exists but not detailed)
- Multiple capture methods with fallback (CopyFromScreen → BitBlt → PrintWindow)

#### 2. Template Matching System ✅ COMPLETE
- **TemplateMatching.cs**: OpenCV-based template matching engine
- Support for confidence-based matching with adjustable thresholds
- Optimized version with pre-converted templates for performance
- Real-time template detection with bounding box visualization

#### 3. ROI (Region of Interest) System ✅ COMPLETE
- Interactive ROI selection via mouse drag
- JSON-based ROI configuration persistence
- Multiple ROI windows for parallel monitoring
- Real-time ROI capture with separate windows

#### 4. HP/MP Detection System ✅ COMPLETE
- **HPMPDetector.cs**: Specialized color-based health/mana detection
- Horizontal scanline analysis for bar reading
- Configurable threshold warnings
- Visual status indicators with color coding

#### 5. Status Panel ✅ COMPLETE
- **StatusPanel.cs**: Dedicated monitoring dashboard
- Real-time HP/MP bar visualization
- Template match counter
- Floating panel with topmost property

### Key Features Implemented

#### User Interface
- Window selection dropdown with refresh capability
- Real-time capture preview with zoom mode
- Multiple capture modes (continuous, single, ROI-based)
- Interactive ROI/template selection via mouse drag
- Debug log panel with timestamp and auto-scroll
- Performance metrics display (FPS, capture time)

#### Template Management
- Template creation from captured regions
- Template loading/saving functionality
- ROI-to-template mapping system
- Multi-template matching per ROI
- Visual match indicators with confidence scores

#### Performance Optimizations
- 100ms capture intervals (10 FPS)
- Pre-converted template caching
- Efficient memory management with disposal
- Parallel ROI processing

### Technical Stack
- **Framework**: .NET Windows Forms
- **Computer Vision**: OpenCvSharp (OpenCV for .NET)
- **Image Processing**: System.Drawing
- **Configuration**: Newtonsoft.Json
- **Target Platform**: Windows Desktop

### File Structure Analysis
```
D:\macro\src\MapleViewCapture\
├── MainForm.cs              [Main UI orchestration - 1500+ lines]
├── TemplateMatching.cs      [OpenCV template matching engine]  
├── HPMPDetector.cs          [Health/mana bar detection]
├── ScreenCapture.cs         [Window capture utilities]
├── StatusPanel.cs           [Monitoring dashboard UI]
├── Program.cs               [Application entry point]
├── Form1.cs                 [Legacy/backup form]
├── AdvancedCapture.cs       [Advanced capture methods]
├── SimpleForm.cs            [Simplified UI version]
└── MapleViewCapture.csproj  [Project configuration]
```

### Current Functional Capabilities

#### Capture Operations
- ✅ Real-time window capture (10 FPS)
- ✅ Manual single-shot capture  
- ✅ ROI-based region monitoring
- ✅ Multiple capture method fallbacks

#### Analysis Features  
- ✅ Template matching with confidence scoring
- ✅ HP/MP bar percentage detection
- ✅ Multi-ROI parallel processing
- ✅ Performance monitoring

#### User Experience
- ✅ Interactive region selection
- ✅ Real-time visual feedback
- ✅ Configurable thresholds
- ✅ Debug logging system
- ✅ Floating status panels

### Development Maturity Assessment
**Overall: 85-90% Complete**

- **Core Engine**: Fully implemented ✅
- **UI/UX**: Well-developed with comprehensive controls ✅  
- **Performance**: Optimized for real-time operation ✅
- **Configuration**: JSON-based persistence ✅
- **Error Handling**: Comprehensive exception management ✅
- **Documentation**: Code comments present, external docs needed ⚠️

### Next Development Phase Recommendations

#### Immediate Enhancements (10-15% remaining)
1. **Automation Layer**: Action triggering based on detection results
2. **Enhanced Templates**: More sophisticated matching algorithms  
3. **Configuration UI**: GUI for threshold/parameter adjustment
4. **Export/Import**: Template and ROI sharing capabilities
5. **Logging System**: File-based logging for analysis

#### Potential Advanced Features
1. **Machine Learning**: AI-based object detection
2. **Network Capability**: Remote monitoring/control
3. **Scripting Engine**: User-defined automation scripts
4. **Database Integration**: Match result historical tracking

## Conclusion
This is a **mature, well-architected application** with robust core functionality. The codebase demonstrates professional-level software engineering with proper separation of concerns, error handling, and performance optimization. The project is ready for production use with minor enhancements.