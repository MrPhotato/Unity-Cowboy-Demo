# Beat Detector

*[English](#english) | [中文](#chinese)*

<a name="english"></a>

## Beat Detector - Rhythm Analysis Tool for Game Development

### Introduction
Beat Detector is a specialized audio analysis tool that extracts rhythm patterns from music files and exports them in JSON format for integration with rhythm-based game mechanics. Developed primarily to support the "Bullet Time" rhythm combat system in a Unity RPG game, this tool offers precise beat timing extraction for synchronized gameplay elements.

### Features
- **Accurate Beat Detection**: Identifies beats in music tracks using audio signal processing algorithms
- **Visual Waveform Analysis**: Displays audio waveform with beat markers for visual confirmation
- **Interactive Preview**: Play the audio with beat sounds to verify detection accuracy
- **Adjustable Sensitivity**: Fine-tune beat detection parameters through an intuitive interface
- **JSON Export**: Generates optimized data format ready for game engine integration
- **Real-time Feedback**: Immediate visual and auditory feedback during configuration

### Technology Stack
- **Librosa**: Core library for audio analysis and beat detection
- **NumPy**: Numerical processing for audio data
- **Tkinter**: GUI framework for the application interface
- **Pygame**: Audio playback with precise timing control
- **Matplotlib**: Waveform visualization with beat markers
- **JSON**: Structured data export for game engine compatibility
- **Threading**: Non-blocking audio processing and playback

### Requirements
- Python 3.7+
- Librosa
- NumPy
- Pygame
- Matplotlib
- Soundfile

```
pip install librosa numpy pygame matplotlib soundfile
```

### Usage
1. Launch the application: `python beat_detector.py`
2. Load an audio file using the "Open File" button
3. Adjust parameters if needed:
   - **Sensitivity**: Controls detection threshold (lower = more beats)
   - **Tempo Range**: Constrains the BPM search range
4. Click "Analyze" to process the audio
5. Use "Play" to verify detected beats with audible clicks
6. Export the beat data to JSON using "Save Beat Data"

### Output Format
The generated JSON file contains:
```json
{
  "file_name": "track_name.mp3",
  "tempo": 120.5,
  "beats": [0.5, 1.0, 1.5, 2.0, ...],
  "total_beats": 150,
  "sensitivity_setting": 0.8
}
```

### Application in Games
This tool was specifically developed for the "Unity Rhythm RPG Demo" to enable:
- Synchronized combat actions with music beats
- Dynamic difficulty adjustment based on rhythm accuracy
- Energy accumulation system tied to player timing precision
- Real-time feedback with visual effects tied to the music's rhythm

---

<a name="chinese"></a>

## 节拍检测器 - 游戏开发节奏分析工具

### 简介
节拍检测器是一款专门的音频分析工具，可从音乐文件中提取节奏模式并以JSON格式导出，以便与基于节奏的游戏机制集成。该工具主要用于支持Unity RPG游戏中的"子弹时间"节奏战斗系统，提供精确的节拍时间提取，实现同步的游戏元素。

### 功能特点
- **精确节拍检测**：使用音频信号处理算法识别音乐轨道中的节拍
- **可视化波形分析**：显示带有节拍标记的音频波形，便于视觉确认
- **交互式预览**：播放带有节拍提示音的音频，以验证检测精度
- **可调节灵敏度**：通过直观的界面微调节拍检测参数
- **JSON导出**：生成优化的数据格式，可直接用于游戏引擎集成
- **实时反馈**：在配置过程中提供即时的视觉和听觉反馈

### 技术栈
- **Librosa**：音频分析和节拍检测的核心库
- **NumPy**：音频数据的数值处理
- **Tkinter**：应用程序界面的GUI框架
- **Pygame**：具有精确计时控制的音频播放
- **Matplotlib**：带有节拍标记的波形可视化
- **JSON**：结构化数据导出，兼容游戏引擎
- **Threading**：非阻塞式音频处理和播放

### 环境要求
- Python 3.7+
- Librosa
- NumPy
- Pygame
- Matplotlib
- Soundfile

```
pip install librosa numpy pygame matplotlib soundfile
```

### 使用方法
1. 启动应用程序：`python beat_detector.py`
2. 使用"打开文件"按钮加载音频文件
3. 根据需要调整参数：
   - **灵敏度**：控制检测阈值（较低 = 更多节拍）
   - **速度范围**：限制BPM搜索范围
4. 点击"分析"处理音频
5. 使用"播放"通过可听见的点击声验证检测到的节拍
6. 使用"保存节拍数据"将节拍数据导出为JSON

### 输出格式
生成的JSON文件包含：
```json
{
  "file_name": "音轨名称.mp3",
  "tempo": 120.5,
  "beats": [0.5, 1.0, 1.5, 2.0, ...],
  "total_beats": 150,
  "sensitivity_setting": 0.8
}
```

### 在游戏中的应用
该工具专门为"Unity节奏RPG Demo"开发，用于实现：
- 将战斗动作与音乐节拍同步
- 基于节奏准确性的动态难度调整
- 与玩家计时精度相关的能量积累系统
- 与音乐节奏绑定的视觉效果实时反馈 