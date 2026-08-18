# 定时电源助手

一个简洁、完全离线的中文 Windows 定时电源工具。支持按倒计时或指定时刻执行关机、重启、睡眠、休眠和锁定。

## 功能

- 倒计时或指定每天的某个时刻
- 关机、重启、睡眠、休眠、锁定
- 可选强制关闭未响应程序
- 倒计时窗口置顶、最小化到系统托盘
- 剩余时间分级颜色提醒
- 简单命令行参数，便于脚本调用
- 不联网、不收集数据

> [!WARNING]
> “强制关闭”可能导致未保存的内容丢失。运行定时任务前请保存工作。

## 下载和使用

在仓库右侧 **Releases** 下载 `ZhutdownTimer.zip`，解压后直接运行 `ZhutdownTimer.exe`。系统要求为 Windows 10/11 和 .NET Framework 4.8（现代 Windows 通常已包含）。

也可以下载源码后在 PowerShell 中运行：

```powershell
.\build.ps1
```

生成文件位于 `dist\ZhutdownTimer.exe`。

## 命令行

```text
ZhutdownTimer.exe --seconds 3600 --action shutdown --start
ZhutdownTimer.exe --seconds 1800 --action sleep --start --background
```

可用操作：`shutdown`、`restart`、`sleep`、`hibernate`、`lock`。

## 关于“定时开机”

完全关机后 Windows 程序无法继续运行，因此本工具不能让已彻底关机的电脑自行开机。请使用主板 BIOS/UEFI 的 `RTC Alarm` / `Power On By RTC` 功能，或由局域网内另一台常开设备发送 Wake-on-LAN。

## 从源码构建

- 本地快速构建：Windows 自带的 .NET Framework C# 编译器，运行 `build.ps1`
- Visual Studio：打开 `ZhutdownTimer.sln`，使用 Release 配置构建
- CI：推送标签 `v*` 后，GitHub Actions 自动构建并创建 Release

## 灵感与许可

本项目的产品思路受到 [Shutdown Timer Classic](https://github.com/lukaslangrock/ShutdownTimerClassic) 启发，并使用独立编写的中文界面与实现。参考项目采用 MIT License；本项目同样使用 [MIT License](LICENSE)。
