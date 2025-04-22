import os
import json
import time
import threading
import numpy as np
import librosa
import soundfile as sf
import pygame
import tkinter as tk
from tkinter import filedialog, messagebox, Scale, HORIZONTAL, scrolledtext, Listbox, Toplevel, Entry, Button, IntVar, DoubleVar, Frame
from matplotlib.figure import Figure
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg, NavigationToolbar2Tk
from matplotlib.backend_bases import MouseButton


class BeatDetectorApp:
    def __init__(self, root):
        self.root = root
        self.root.title("音乐节拍检测器")
        self.root.geometry("1000x700")
        
        # 初始化pygame音频
        pygame.mixer.init()
        
        # 节拍选择模式 (1=添加, 2=删除)
        self.edit_mode = IntVar(value=1)
        
        # 创建界面
        self.create_widgets()
        
        # 存储数据
        self.audio_path = None
        self.beat_times = None
        self.sr = None
        self.y = None
        
        # 播放状态
        self.is_playing = False
        self.is_paused = False
        self.playback_thread = None
        self.play_start_time = 0
        self.pause_time = 0
        self.total_pause_time = 0
        
        # 播放指针
        self.playback_line = None
        self.playback_position = 0
        self.playback_update_id = None
        
        # 加载提示音
        self._load_click_sound()
        
        # 手动编辑状态
        self.editing_mode = False
        self.canvas = None
        self.edit_clicks = []
        
        # 波形图显示范围控制
        self.view_start = 0  # 显示起始时间(秒)
        self.view_duration = 10  # 显示持续时长(秒)
    
    def _load_click_sound(self):
        """加载节拍提示音"""
        # 创建一个简单的"咔嗒"声作为提示音
        sample_rate = 22050
        duration = 0.05  # 50毫秒的短音
        t = np.linspace(0, duration, int(sample_rate * duration), False)
        click = 0.5 * np.sin(2 * np.pi * 1000 * t) * np.exp(-10 * t)
        
        # 将提示音保存为临时文件
        temp_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "temp")
        os.makedirs(temp_dir, exist_ok=True)
        self.click_sound_path = os.path.join(temp_dir, "click.wav")
        sf.write(self.click_sound_path, click, sample_rate)
        
    def create_widgets(self):
        # 创建顶部按钮区域
        btn_frame = tk.Frame(self.root)
        btn_frame.pack(pady=10)
        
        # 选择文件按钮
        self.select_btn = tk.Button(btn_frame, text="选择音乐文件", command=self.select_file)
        self.select_btn.pack(side=tk.LEFT, padx=5)
        
        # 分析按钮
        self.analyze_btn = tk.Button(btn_frame, text="分析节拍", command=self.analyze_beats)
        self.analyze_btn.pack(side=tk.LEFT, padx=5)
        
        # 回放按钮
        self.play_btn = tk.Button(btn_frame, text="回放音乐+节拍", command=self.toggle_playback)
        self.play_btn.pack(side=tk.LEFT, padx=5)
        
        # 编辑节拍按钮
        self.edit_btn = tk.Button(btn_frame, text="手动编辑节拍", command=self.toggle_edit_mode)
        self.edit_btn.pack(side=tk.LEFT, padx=5)
        
        # 导出JSON按钮
        self.export_btn = tk.Button(btn_frame, text="导出为JSON", command=self.export_json)
        self.export_btn.pack(side=tk.LEFT, padx=5)
        
        # 文件路径标签
        self.file_label = tk.Label(self.root, text="未选择文件")
        self.file_label.pack(pady=5)
        
        # 参数调整区域
        param_frame = tk.Frame(self.root)
        param_frame.pack(fill=tk.X, padx=10, pady=5)
        
        # 灵敏度滑块
        sensitivity_frame = Frame(param_frame)
        sensitivity_frame.pack(side=tk.LEFT, padx=10)
        tk.Label(sensitivity_frame, text="节拍检测灵敏度:").pack(side=tk.LEFT)
        self.sensitivity = tk.DoubleVar(value=0.35)
        self.sensitivity_scale = Scale(sensitivity_frame, from_=0.01, to=5.0, 
                                      resolution=0.01, orient=HORIZONTAL, 
                                      variable=self.sensitivity, length=200)
        self.sensitivity_scale.pack(side=tk.LEFT, padx=5)
        
        # 创建主内容区域(左侧波形图和右侧节拍列表)
        main_content = tk.PanedWindow(self.root, orient=tk.HORIZONTAL)
        main_content.pack(fill=tk.BOTH, expand=True, padx=10, pady=5)
        
        # 左侧面板（包含波形图和控制滑块）
        left_panel = Frame(main_content)
        main_content.add(left_panel)
        
        # 波形图控制区域
        view_control_frame = Frame(left_panel)
        view_control_frame.pack(fill=tk.X, pady=5)
        
        # 波形图起始位置滑块
        tk.Label(view_control_frame, text="显示起始位置 (秒):").pack(side=tk.LEFT)
        self.view_start_var = DoubleVar(value=0.0)
        self.view_start_scale = Scale(view_control_frame, from_=0, to=100, 
                                    resolution=0.1, orient=HORIZONTAL, 
                                    variable=self.view_start_var, length=200,
                                    command=self.update_view_range)
        self.view_start_scale.pack(side=tk.LEFT, padx=5)
        
        # 波形图显示长度滑块
        tk.Label(view_control_frame, text="显示长度 (秒):").pack(side=tk.LEFT)
        self.view_duration_var = DoubleVar(value=10.0)
        self.view_duration_scale = Scale(view_control_frame, from_=1, to=60, 
                                       resolution=1, orient=HORIZONTAL, 
                                       variable=self.view_duration_var, length=150,
                                       command=self.update_view_range)
        self.view_duration_scale.pack(side=tk.LEFT, padx=5)
        
        # 编辑模式单选按钮
        edit_mode_frame = Frame(left_panel)
        edit_mode_frame.pack(fill=tk.X, pady=2)
        self.radio_add = tk.Radiobutton(edit_mode_frame, text="添加节拍", variable=self.edit_mode, value=1)
        self.radio_add.pack(side=tk.LEFT, padx=10)
        self.radio_delete = tk.Radiobutton(edit_mode_frame, text="删除节拍", variable=self.edit_mode, value=2)
        self.radio_delete.pack(side=tk.LEFT, padx=10)
        
        # 左侧波形显示区域
        self.plot_frame = tk.Frame(left_panel)
        self.plot_frame.pack(fill=tk.BOTH, expand=True)
        
        # 右侧节拍列表区域
        beats_frame = tk.Frame(main_content)
        main_content.add(beats_frame)
        
        # 设置初始比例
        main_content.paneconfig(left_panel, minsize=400, stretch="always")
        main_content.paneconfig(beats_frame, minsize=150)
        
        # 节拍列表标题
        tk.Label(beats_frame, text="节拍时间点 (秒)").pack(pady=5)
        
        # 节拍列表及其滚动条
        beats_scroll = tk.Scrollbar(beats_frame)
        beats_scroll.pack(side=tk.RIGHT, fill=tk.Y)
        
        self.beats_listbox = Listbox(beats_frame, yscrollcommand=beats_scroll.set, width=20)
        self.beats_listbox.pack(fill=tk.BOTH, expand=True)
        beats_scroll.config(command=self.beats_listbox.yview)
        
        # 节拍编辑按钮区域
        beats_edit_frame = tk.Frame(beats_frame)
        beats_edit_frame.pack(fill=tk.X, pady=5)
        
        self.add_beat_btn = tk.Button(beats_edit_frame, text="添加", command=self.add_beat)
        self.add_beat_btn.pack(side=tk.LEFT, padx=2, fill=tk.X, expand=True)
        
        self.edit_beat_btn = tk.Button(beats_edit_frame, text="编辑", command=self.edit_beat)
        self.edit_beat_btn.pack(side=tk.LEFT, padx=2, fill=tk.X, expand=True)
        
        self.remove_beat_btn = tk.Button(beats_edit_frame, text="删除", command=self.remove_beat)
        self.remove_beat_btn.pack(side=tk.LEFT, padx=2, fill=tk.X, expand=True)
        
        # 状态栏
        self.status_var = tk.StringVar()
        self.status_var.set("准备就绪")
        self.status_bar = tk.Label(self.root, textvariable=self.status_var, bd=1, relief=tk.SUNKEN, anchor=tk.W)
        self.status_bar.pack(side=tk.BOTTOM, fill=tk.X)
    
    def update_view_range(self, *args):
        """更新波形图显示范围"""
        if self.y is not None and hasattr(self, 'canvas') and self.canvas is not None:
            self.view_start = self.view_start_var.get()
            self.view_duration = self.view_duration_var.get()
            
            # 防止超出音频范围
            max_time = len(self.y) / self.sr
            if self.view_start + self.view_duration > max_time:
                self.view_start = max(0, max_time - self.view_duration)
                self.view_start_var.set(self.view_start)
            
            # 更新波形图
            self.redraw_waveform()
    
    def redraw_waveform(self):
        """重绘波形图和节拍标记"""
        if hasattr(self, 'ax') and hasattr(self, 'canvas') and self.canvas is not None:
            # 计算显示范围
            start_sample = int(self.view_start * self.sr)
            end_sample = min(len(self.y), int((self.view_start + self.view_duration) * self.sr))
            
            # 清除当前图像
            self.ax.clear()
            
            # 绘制波形
            times = np.arange(start_sample, end_sample) / self.sr
            self.ax.plot(times, self.y[start_sample:end_sample], alpha=0.5)
            
            # 绘制节拍标记（如果有）
            if self.beat_times is not None and len(self.beat_times) > 0:
                beat_height = np.max(np.abs(self.y[start_sample:end_sample])) * 0.9
                
                # 只绘制当前视图范围内的节拍
                visible_beats = self.beat_times[
                    (self.beat_times >= self.view_start) & 
                    (self.beat_times <= self.view_start + self.view_duration)
                ]
                
                if len(visible_beats) > 0:
                    self.ax.vlines(visible_beats, -beat_height, beat_height, color='r', alpha=0.8)
            
            # 重绘播放指针（如果正在播放）
            if (self.is_playing or self.is_paused) and self.playback_position >= self.view_start and self.playback_position <= self.view_start + self.view_duration:
                beat_height = np.max(np.abs(self.y[start_sample:end_sample])) * 0.9
                self.playback_line = self.ax.axvline(x=self.playback_position, color='g', linestyle='-', linewidth=2)
                
            # 设置图像属性
            self.ax.set_xlim(self.view_start, self.view_start + self.view_duration)
            self.ax.set_xlabel('时间 (秒)')
            self.ax.set_ylabel('振幅')
            self.ax.set_title(f'音频波形和节拍标记 [{self.view_start:.1f}s - {self.view_start + self.view_duration:.1f}s]')
            
            # 更新画布
            self.canvas.draw()
    
    def select_file(self):
        """选择音乐文件"""
        file_path = filedialog.askopenfilename(
            title="选择音乐文件",
            filetypes=[("音频文件", "*.mp3 *.wav *.ogg *.flac")]
        )
        
        if file_path:
            # 停止正在播放的音乐
            self.stop_playback()
            
            self.audio_path = file_path
            self.file_label.config(text=f"已选择: {os.path.basename(file_path)}")
            self.status_var.set(f"已加载文件: {os.path.basename(file_path)}")
            # 重置数据
            self.beat_times = None
            self.y = None
            self.sr = None
            # 清除图表
            for widget in self.plot_frame.winfo_children():
                widget.destroy()
            # 清除节拍列表
            self.beats_listbox.delete(0, tk.END)
    
    def analyze_beats(self):
        """分析音频文件并检测节拍"""
        if not self.audio_path:
            messagebox.showerror("错误", "请先选择一个音频文件")
            return
        
        try:
            self.status_var.set("正在分析音频...")
            self.root.update()
            
            # 加载音频文件（如果还没加载）
            if self.y is None or self.sr is None:
                y, sr = librosa.load(self.audio_path, sr=None)
                self.y = y
                self.sr = sr
                
                # 更新波形图滑块最大值
                max_time = len(self.y) / self.sr
                self.view_start_scale.config(to=max(0, max_time - 1))
                self.view_duration_scale.config(to=min(max_time, 60))
            
            # 获取当前敏感度
            sensitivity = self.sensitivity.get()
            
            # 检测节拍
            tempo, beat_frames = librosa.beat.beat_track(y=self.y, sr=self.sr, 
                                                        tightness=sensitivity)
            self.beat_times = librosa.frames_to_time(beat_frames, sr=self.sr)
            
            # 显示波形和节拍
            self.plot_waveform_and_beats()
            
            # 更新节拍列表
            self.update_beats_list()
            
            self.status_var.set(f"分析完成。检测到 {len(self.beat_times)} 个节拍，平均节奏: {float(tempo):.1f} BPM (灵敏度: {sensitivity})")
        except Exception as e:
            messagebox.showerror("错误", f"分析失败: {str(e)}")
            self.status_var.set("分析失败")
    
    def plot_waveform_and_beats(self):
        """绘制波形和节拍标记"""
        # 清除原有图表
        for widget in self.plot_frame.winfo_children():
            widget.destroy()
        
        # 创建图表
        fig = Figure(figsize=(8, 4), dpi=100)
        self.ax = fig.add_subplot(111)
        
        # 设置音频总长度
        if self.y is not None and self.sr is not None:
            max_time = len(self.y) / self.sr
            
            # 设置默认视图范围
            self.view_start = 0
            self.view_duration = min(max_time, 10)
            self.view_start_var.set(self.view_start)
            self.view_duration_var.set(self.view_duration)
            
            # 更新波形图滑块最大值
            self.view_start_scale.config(to=max(0, max_time - 1))
            self.view_duration_scale.config(to=min(max_time, 60))
        
        # 调用波形图更新函数
        self.redraw_waveform()
        
        # 显示图表
        self.canvas = FigureCanvasTkAgg(fig, master=self.plot_frame)
        self.canvas.draw()
        canvas_widget = self.canvas.get_tk_widget()
        canvas_widget.pack(fill=tk.BOTH, expand=True)
        
        # 添加导航工具栏
        toolbar = NavigationToolbar2Tk(self.canvas, self.plot_frame)
        toolbar.update()
        
        # 绑定鼠标点击事件（用于编辑模式）
        self.canvas.mpl_connect('button_press_event', self.on_plot_click)
    
    def on_plot_click(self, event):
        """处理波形图的点击事件"""
        if not self.editing_mode or event.button != MouseButton.LEFT:
            return
        
        if event.xdata is not None:
            click_time = event.xdata
            
            # 编辑模式为1(添加)
            if self.edit_mode.get() == 1:
                # 添加新节拍点
                if self.beat_times is None:
                    self.beat_times = np.array([click_time])
                else:
                    self.beat_times = np.append(self.beat_times, click_time)
                    self.beat_times = np.sort(self.beat_times)
                
                self.status_var.set(f"添加了新节拍点: {click_time:.2f}秒")
            
            # 编辑模式为2(删除)
            elif self.edit_mode.get() == 2 and self.beat_times is not None and len(self.beat_times) > 0:
                # 查找距离点击位置最近的节拍
                distances = np.abs(self.beat_times - click_time)
                nearest_idx = np.argmin(distances)
                nearest_distance = distances[nearest_idx]
                
                # 如果距离小于阈值(0.5秒)，删除该节拍
                if nearest_distance < 0.5:
                    deleted_time = self.beat_times[nearest_idx]
                    self.beat_times = np.delete(self.beat_times, nearest_idx)
                    self.status_var.set(f"删除了节拍点: {deleted_time:.2f}秒")
                else:
                    self.status_var.set(f"附近没有节拍点可删除 (最近的点距离: {nearest_distance:.2f}秒)")
            
            # 重新绘制节拍标记和更新列表
            self.redraw_waveform()
            self.update_beats_list()
    
    def update_beats_list(self):
        """更新右侧节拍列表"""
        self.beats_listbox.delete(0, tk.END)
        if self.beat_times is not None:
            for i, beat_time in enumerate(self.beat_times):
                self.beats_listbox.insert(tk.END, f"{i+1}. {beat_time:.3f}秒")
    
    def toggle_edit_mode(self):
        """切换节拍编辑模式"""
        if not self.audio_path:
            messagebox.showerror("错误", "请先选择一个音频文件")
            return
        
        if self.y is None:
            messagebox.showerror("错误", "请先分析音频或手动创建节拍")
            return
        
        self.editing_mode = not self.editing_mode
        
        if self.editing_mode:
            self.edit_btn.config(text="退出编辑模式")
            self.status_var.set("编辑模式已启用 - 点击波形图添加/删除节拍点，使用右侧列表管理节拍")
            
            # 启用编辑模式单选按钮
            self.radio_add.config(state=tk.NORMAL)
            self.radio_delete.config(state=tk.NORMAL)
            
            # 如果还没有节拍数据，创建空数组
            if self.beat_times is None:
                self.beat_times = np.array([])
                self.plot_waveform_and_beats()
        else:
            self.edit_btn.config(text="手动编辑节拍")
            self.status_var.set("编辑模式已关闭")
            
            # 禁用编辑模式单选按钮
            self.radio_add.config(state=tk.DISABLED)
            self.radio_delete.config(state=tk.DISABLED)
    
    def add_beat(self):
        """通过对话框添加节拍"""
        if not self.audio_path or self.y is None:
            messagebox.showerror("错误", "请先选择并分析音频文件")
            return
        
        if self.beat_times is None:
            self.beat_times = np.array([])
        
        # 创建添加节拍对话框
        add_dialog = Toplevel(self.root)
        add_dialog.title("添加节拍")
        add_dialog.geometry("300x100")
        add_dialog.resizable(False, False)
        add_dialog.transient(self.root)
        add_dialog.grab_set()
        
        # 对话框内容
        tk.Label(add_dialog, text="输入节拍时间 (秒):").pack(pady=5)
        time_entry = Entry(add_dialog)
        time_entry.pack(fill=tk.X, padx=20, pady=5)
        time_entry.focus_set()
        
        # 确认按钮
        def on_confirm():
            try:
                beat_time = float(time_entry.get())
                if beat_time < 0 or beat_time > len(self.y) / self.sr:
                    messagebox.showerror("错误", "节拍时间超出音频范围")
                    return
                
                self.beat_times = np.append(self.beat_times, beat_time)
                self.beat_times = np.sort(self.beat_times)
                self.redraw_waveform()
                self.update_beats_list()
                add_dialog.destroy()
            except ValueError:
                messagebox.showerror("错误", "请输入有效的数字")
        
        Button(add_dialog, text="确认", command=on_confirm).pack(pady=10)
    
    def edit_beat(self):
        """编辑选中的节拍"""
        if self.beat_times is None or len(self.beat_times) == 0:
            messagebox.showerror("错误", "没有节拍数据可编辑")
            return
        
        # 获取选中的索引
        selected = self.beats_listbox.curselection()
        if not selected:
            messagebox.showerror("错误", "请先选择一个节拍")
            return
        
        index = selected[0]
        current_time = self.beat_times[index]
        
        # 创建编辑对话框
        edit_dialog = Toplevel(self.root)
        edit_dialog.title("编辑节拍")
        edit_dialog.geometry("300x100")
        edit_dialog.resizable(False, False)
        edit_dialog.transient(self.root)
        edit_dialog.grab_set()
        
        # 对话框内容
        tk.Label(edit_dialog, text="编辑节拍时间 (秒):").pack(pady=5)
        time_entry = Entry(edit_dialog)
        time_entry.insert(0, f"{current_time:.3f}")
        time_entry.pack(fill=tk.X, padx=20, pady=5)
        time_entry.focus_set()
        
        # 确认按钮
        def on_confirm():
            try:
                beat_time = float(time_entry.get())
                if beat_time < 0 or beat_time > len(self.y) / self.sr:
                    messagebox.showerror("错误", "节拍时间超出音频范围")
                    return
                
                # 更新节拍时间
                self.beat_times[index] = beat_time
                self.beat_times = np.sort(self.beat_times)
                self.redraw_waveform()
                self.update_beats_list()
                edit_dialog.destroy()
            except ValueError:
                messagebox.showerror("错误", "请输入有效的数字")
        
        Button(edit_dialog, text="确认", command=on_confirm).pack(pady=10)
    
    def remove_beat(self):
        """删除选中的节拍"""
        if self.beat_times is None or len(self.beat_times) == 0:
            messagebox.showerror("错误", "没有节拍数据可删除")
            return
        
        # 获取选中的索引
        selected = self.beats_listbox.curselection()
        if not selected:
            messagebox.showerror("错误", "请先选择一个节拍")
            return
        
        index = selected[0]
        
        # 删除节拍
        self.beat_times = np.delete(self.beat_times, index)
        self.redraw_waveform()
        self.update_beats_list()
        self.status_var.set(f"已删除节拍点")
    
    def toggle_playback(self):
        """切换音乐播放状态"""
        if not self.audio_path:
            messagebox.showerror("错误", "请先选择一个音频文件")
            return
        
        if self.is_playing:
            self.pause_playback()
        elif self.is_paused:
            self.resume_playback()
        else:
            self.start_playback()
    
    def start_playback(self):
        """开始播放音乐和节拍提示音"""
        try:
            # 停止任何现有的播放
            self.stop_playback(update_ui=False)
            
            self.is_playing = True
            self.is_paused = False
            self.play_btn.config(text="暂停回放")
            self.playback_position = 0
            self.total_pause_time = 0
            
            # 创建并启动播放线程
            self.playback_thread = threading.Thread(target=self.playback_worker)
            self.playback_thread.daemon = True
            self.playback_thread.start()
            
            # 启动播放位置指针更新
            self.update_playback_pointer()
            
        except Exception as e:
            messagebox.showerror("播放错误", str(e))
            self.stop_playback()
    
    def pause_playback(self):
        """暂停播放"""
        if self.is_playing:
            self.is_playing = False
            self.is_paused = True
            self.play_btn.config(text="继续回放")
            self.pause_time = time.time()
            
            # 暂停音乐
            pygame.mixer.music.pause()
            
            self.status_var.set(f"已暂停 - 位置: {self.playback_position:.2f}秒")
    
    def resume_playback(self):
        """继续播放"""
        if self.is_paused:
            self.is_playing = True
            self.is_paused = False
            self.play_btn.config(text="暂停回放")
            
            # 计算暂停的时间
            pause_duration = time.time() - self.pause_time
            self.total_pause_time += pause_duration
            
            # 继续播放音乐
            pygame.mixer.music.unpause()
            
            self.status_var.set("继续播放音乐和节拍提示音")
    
    def stop_playback(self, update_ui=True):
        """完全停止播放"""
        self.is_playing = False
        self.is_paused = False
        if update_ui:
            self.play_btn.config(text="回放音乐+节拍")
        
        # 取消指针更新计时器
        if self.playback_update_id is not None:
            self.root.after_cancel(self.playback_update_id)
            self.playback_update_id = None
        
        # 停止pygame播放
        pygame.mixer.music.stop()
        pygame.mixer.stop()
        
        # 清除播放指针
        self.playback_position = 0
        self.redraw_waveform()
    
    def update_playback_pointer(self):
        """更新播放指针位置"""
        if self.is_playing or self.is_paused:
            # 如果当前正在播放或已暂停
            if self.is_playing:
                # 使用pygame获取实际播放位置（毫秒）
                pygame_pos = pygame.mixer.music.get_pos()
                if pygame_pos >= 0:  # 确保返回有效值
                    # 将毫秒转换为秒，并加上暂停的累计时间
                    self.playback_position = pygame_pos / 1000.0
            
            # 检查是否需要切换视图范围
            if (self.playback_position < self.view_start or 
                self.playback_position > self.view_start + self.view_duration):
                # 播放位置超出当前视图，自动滚动
                if self.is_playing:  # 仅在播放状态下自动滚动
                    self.view_start = max(0, self.playback_position - self.view_duration * 0.1)
                    self.view_start_var.set(self.view_start)
                    self.update_view_range()
            else:
                # 仅更新播放指针
                if hasattr(self, 'ax') and hasattr(self, 'canvas') and self.canvas is not None:
                    if hasattr(self, 'playback_line') and self.playback_line:
                        self.playback_line.remove()
                    
                    beat_height = np.max(np.abs(self.y)) * 0.9
                    self.playback_line = self.ax.axvline(x=self.playback_position, color='g', linestyle='-', linewidth=2)
                    self.canvas.draw()
            
            # 设置下一次更新
            self.playback_update_id = self.root.after(100, self.update_playback_pointer)  # 每100毫秒更新一次
    
    def playback_worker(self):
        """播放线程的工作函数"""
        try:
            # 初始化时间
            self.play_start_time = time.time()
            
            # 加载并播放音乐
            pygame.mixer.music.load(self.audio_path)
            pygame.mixer.music.play()
            
            # 加载节拍提示音
            if os.path.exists(self.click_sound_path):
                click_sound = pygame.mixer.Sound(self.click_sound_path)
            else:
                self._load_click_sound()
                click_sound = pygame.mixer.Sound(self.click_sound_path)
            
            # 如果没有节拍数据，就只播放音乐
            if self.beat_times is None or len(self.beat_times) == 0:
                self.status_var.set("正在播放音乐（无节拍数据）")
                
                # 等待音乐播放完毕
                while (pygame.mixer.music.get_busy() and self.is_playing) or self.is_paused:
                    time.sleep(0.1)
            else:
                # 播放音乐和提示音
                self.status_var.set("正在播放音乐和节拍提示音")
                
                # 当前处理到的节拍索引
                current_beat_index = 0
                
                # 上次更新状态的时间
                last_status_update = 0
                
                # 播放循环
                while ((pygame.mixer.music.get_busy() and self.is_playing) or self.is_paused) and current_beat_index < len(self.beat_times):
                    if self.is_playing:  # 只在播放状态下更新
                        # 使用pygame获取实际播放位置（毫秒）
                        pygame_pos = pygame.mixer.music.get_pos()
                        
                        if pygame_pos >= 0:  # 确保返回有效值
                            # 将毫秒转换为秒
                            current_time = pygame_pos / 1000.0
                            
                            # 检查是否应该播放提示音
                            while (current_beat_index < len(self.beat_times) and 
                                  current_time >= self.beat_times[current_beat_index]):
                                # 播放提示音
                                click_sound.play()
                                
                                # 限制状态更新频率
                                if time.time() - last_status_update > 0.2:
                                    beat_time = self.beat_times[current_beat_index]
                                    self.status_var.set(f"播放中: 节拍 {current_beat_index+1}/{len(self.beat_times)} ({beat_time:.2f}秒)")
                                    
                                    # 通过主线程更新节拍列表选择
                                    self.root.after(0, lambda idx=current_beat_index: self.update_beat_selection(idx))
                                    
                                    last_status_update = time.time()
                                
                                # 移动到下一个节拍
                                current_beat_index += 1
                    
                    # 短暂休眠，减少CPU占用
                    time.sleep(0.005)
            
            # 播放完成
            if self.is_playing:  # 只有在自然结束时才更新状态
                self.status_var.set("播放完成")
                self.stop_playback()
            
        except Exception as e:
            # 出错时更新UI
            error_msg = str(e)
            self.root.after(0, lambda: messagebox.showerror("播放错误", error_msg))
            self.root.after(0, lambda: self.stop_playback())
    
    def update_beat_selection(self, index):
        """更新节拍列表选择（在主线程中调用）"""
        if index < self.beats_listbox.size():
            self.beats_listbox.selection_clear(0, tk.END)
            self.beats_listbox.selection_set(index)
            self.beats_listbox.see(index)
    
    def export_json(self):
        """将节拍信息导出为JSON文件"""
        if self.beat_times is None:
            messagebox.showerror("错误", "请先分析音频")
            return
        
        # 创建节拍数据
        beat_data = {
            "file_name": os.path.basename(self.audio_path),
            "sample_rate": self.sr,
            "tempo": float(librosa.beat.tempo(y=self.y, sr=self.sr)[0]),
            "beats": self.beat_times.tolist(),
            "total_beats": len(self.beat_times),
            "sensitivity_setting": self.sensitivity.get()
        }
        
        # 请求保存位置
        save_path = filedialog.asksaveasfilename(
            title="保存JSON文件",
            defaultextension=".json",
            filetypes=[("JSON文件", "*.json")]
        )
        
        if save_path:
            try:
                with open(save_path, 'w', encoding='utf-8') as f:
                    json.dump(beat_data, f, indent=2, ensure_ascii=False)
                self.status_var.set(f"节拍数据已保存至 {os.path.basename(save_path)}")
                messagebox.showinfo("成功", "节拍数据已成功导出为JSON文件")
            except Exception as e:
                messagebox.showerror("错误", f"保存失败: {str(e)}")


if __name__ == "__main__":
    root = tk.Tk()
    app = BeatDetectorApp(root)
    root.mainloop() 