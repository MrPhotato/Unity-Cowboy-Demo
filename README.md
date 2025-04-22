# Unity Rhythm RPG Demo

*[English](#english) | [中文](#chinese)*

<a name="english"></a>

## Overview

Unity Rhythm RPG Demo is an experimental game project that combines traditional RPG gameplay with rhythm game mechanics. Set in a mysterious cathedral, the game features exploration, puzzle-solving, and a unique combat system where timing your actions to the beat of the music enhances your combat effectiveness.

## Game Features

### Distinctive Gameplay Mechanics
- **Exploration**: Navigate through detailed environments including a cathedral and its theatre
- **Puzzle-solving**: Arrange story pieces and match emotions to unlock the narrative
- **Rhythm Combat**: Unique "Bullet Time" system that synchronizes combat with music beats
- **Energy System**: Accumulate energy by hitting perfect timing during combat

### Dynamic Audio Experience
- 5 seamlessly switching background music tracks sharing the same BPM, key, and melody
- Dynamic audio mixing based on gameplay context (exploration, puzzle-solving, combat, climax)
- Custom-developed beat detection system for precise rhythm synchronization

### Visually Engaging Environments
- Cathedral exterior with atmospheric lighting
- Interactive puzzle board with emotion-reactive images
- Theatre combat arena with dynamic spotlight systems
- Visual feedback systems tied to rhythm accuracy

## Controls

- **WASD**: Character movement
- **Space**: Jump
- **Shift**: Sprint
- **Ctrl**: Walk
- **Left Mouse Button**: Shoot
- **R** (in Theatre): Activate Bullet Time
- **F** (at Cathedral entrance): Enter building
- **Esc**: Return to previous scene / Exit game

## Scenes

### 1. The Cathedral on the Lawn
An exterior environment where players can explore freely. Climb the steps and press F to enter the Cathedral.

### 2. Puzzle-Solving Board Game
A narrative puzzle where players arrange story pieces in chronological order and match them with appropriate emotions. Success reveals the backstory of why the cowboy came to the cathedral.

### 3. The Theatre inside the Cathedral
A combat arena where players face robotic enemies. Press R to activate the special "Bullet Time" combat mode.

## Technical Implementation

### Core Systems
- **State Machine Architecture**: Modular character controller with distinct states (idle, walk, run, sprint, jump, shoot, etc.)
- **Camera System**: Dynamic transition between standard third-person view and top-down combat view
- **Beat Detection**: Custom-built Python tool to extract rhythm data from music files
- **Music Manager**: Seamless audio track switching based on gameplay context
- **Spotlight System**: Dynamic lighting that responds to player actions and combat performance

### AI-Assisted Development
The project leverages various AI tools to maximize content creation within a limited timeframe:
- **Music Generation**: Custom tracks created with Suno AI
- **Narrative Imagery**: Storyboard illustrations generated using OpenAI's GPT-4o
- **Transition Animations**: Scene transitions created with Vidu 2.0
- **3D Modeling**: Environment elements created with Hyper 3D's AI
- **Level Design**: Scene construction accelerated using MCP technology with Blender

## Story Background

The game follows a cowboy's journey to discover his origins:
1. The protagonist's father was a cloning and mechanical researcher who worked in a cathedral laboratory
2. The father developed feelings for his research subjects and erased their painful experimental memories
3. One subject (the protagonist) escaped to the desert
4. The son became a cowboy like his father, and after his father's death, discovered his true origins
5. He returns to the cathedral to uncover the truth and confront his past

## Installation & Setup

1. Clone the repository or download the project files
2. Open the project in Unity 2022.3 or later
3. Ensure required packages are installed (listed in the package manifest)
4. Open the "Demo" scene as the starting point
5. Press Play to begin

## Future Development Plans

- Additional side quests based on different interpretations of the story
- Enhanced skill progression tied to the energy accumulation system
- Dancing robot enemies performing ballet-like movements on the theatre stage
- Optimization of audio-visual synchronization in Bullet Time mode
- Additional victory conditions and progression mechanics

## Resource Attribution

- **Cowboy Character**: Lowpoly Cowboy RIO V1.1 from Unity Asset Store
- **Robot Enemies**: Robot Kyle | URP from Unity Asset Store
- **Animations**: Basic Motions FREE from Unity Asset Store, with additional animations from Mixamo
- **Music**: Generated with Suno AI
- **Images**: Created with OpenAI GPT-4o
- **Transition Videos**: Generated with Vidu 2.0
- **3D Models**: Created with Poly3D
- **MCP Technology**: Blender MCP (https://github.com/ahujasid/blender-mcp) and Unity MCP (https://github.com/justinpbarnett/unity-mcp)

---

<a name="chinese"></a>

## 概述

Unity节奏RPG Demo是一个将传统RPG游戏玩法与节奏游戏机制相结合的实验性游戏项目。游戏背景设定在一座神秘的大教堂中，包含探索、解谜和独特的战斗系统，玩家可以通过与音乐节拍同步的操作来提升战斗效果。

## 游戏特点

### 独特的游戏机制
- **探索系统**：在精心设计的环境中导航，包括大教堂外观和内部剧场
- **解谜元素**：通过排列故事片段并匹配相应情绪来解锁叙事
- **节奏战斗**：独特的"子弹时间"系统，将战斗与音乐节拍同步
- **能量系统**：在战斗中击中完美时机可积累能量

### 动态音频体验
- 5首无缝切换的背景音乐，共享相同的BPM、调性和旋律
- 基于游戏场景动态调整音频混合（探索、解谜、战斗、高潮）
- 自主开发的节拍检测系统，确保精确的节奏同步

### 视觉上引人入胜的环境
- 大气光照效果的教堂外观
- 带有情绪反应图像的互动解谜板
- 带有动态聚光灯系统的剧场战斗场景
- 与节奏准确度相关的视觉反馈系统

## 控制方式

- **WASD**：角色移动
- **空格键**：跳跃
- **Shift**：冲刺
- **Ctrl**：行走
- **鼠标左键**：射击
- **R键**（在剧场中）：激活子弹时间
- **F键**（在教堂入口）：进入建筑
- **Esc键**：返回上一场景/退出游戏

## 场景

### 1. 草坪上的大教堂
一个玩家可以自由探索的外部环境。爬上台阶并按F键进入大教堂。

### 2. 解谜棋盘游戏
一个叙事解谜环节，玩家需要按时间顺序排列故事片段，并为其匹配适当的情绪。成功解谜后将揭示牛仔来到大教堂的背景故事。

### 3. 大教堂内的剧场
一个战斗场景，玩家将面对机器人敌人。按R键激活特殊的"子弹时间"战斗模式。

## 技术实现

### 核心系统
- **状态机架构**：模块化角色控制器，具有不同状态（闲置、行走、奔跑、冲刺、跳跃、射击等）
- **摄像机系统**：标准第三人称视角和俯视战斗视角之间的动态切换
- **节拍检测**：自主开发的Python工具，用于从音乐文件中提取节奏数据
- **音乐管理器**：基于游戏上下文无缝切换音轨
- **聚光灯系统**：响应玩家行动和战斗表现的动态光照

### AI辅助开发
项目利用各种AI工具在有限时间内最大化内容创作：
- **音乐生成**：使用Suno AI创建的自定义音轨
- **叙事图像**：使用OpenAI的GPT-4o生成的故事板插图
- **过渡动画**：使用Vidu 2.0创建的场景转换
- **3D建模**：使用Hyper 3D的AI创建的环境元素
- **关卡设计**：使用MCP技术结合Blender加速场景构建

## 故事背景

游戏讲述了一名牛仔寻找自己起源的旅程：
1. 主角的父亲是一名克隆和机械研究员，在大教堂实验室工作
2. 父亲对研究对象产生感情，并抹去了他们痛苦的实验记忆
3. 其中一个研究对象（主角）逃到了沙漠
4. 儿子像父亲一样成为了牛仔，在父亲死后发现了自己的真正起源
5. 他回到大教堂揭开真相并面对过去

## 安装与设置

1. 克隆存储库或下载项目文件
2. 在Unity 2022.3或更高版本中打开项目
3. 确保安装所需包（在包清单中列出）
4. 打开"Demo"场景作为起点
5. 按播放键开始游戏

## 未来开发计划

- 基于故事不同解读的额外支线任务
- 与能量积累系统相关的技能进阶系统
- 在剧场舞台上表演芭蕾般动作的舞蹈机器人敌人
- 优化子弹时间模式下的视听同步
- 额外的胜利条件和进阶机制

## 资源归属

- **牛仔角色**：来自Unity资源商店的Lowpoly Cowboy RIO V1.1
- **机器人敌人**：来自Unity资源商店的Robot Kyle | URP
- **动画**：来自Unity资源商店的Basic Motions FREE，以及来自Mixamo的额外动画
- **音乐**：使用Suno AI生成
- **图像**：使用OpenAI GPT-4o创建
- **过渡视频**：使用Vidu 2.0生成
- **3D模型**：使用Poly3D创建
- **MCP技术**：Blender MCP (https://github.com/ahujasid/blender-mcp) 和 Unity MCP (https://github.com/justinpbarnett/unity-mcp) 