# 安装 .NET Framework 开发工具包

## 问题说明

构建项目时出现错误：找不到 .NET Framework 的引用程序集。这是因为系统缺少 .NET Framework 开发工具包。

## 解决方案

### 方法 1：通过 Visual Studio Installer 安装（推荐）

1. 打开 **Visual Studio Installer**
2. 点击 **修改** 按钮
3. 在 **单个组件** 标签页中，搜索 "**.NET Framework 4.8 SDK**" 或 "**.NET Framework 4.8 targeting pack**"
4. 勾选并安装
5. 安装完成后，重新运行 `build.ps1`

### 方法 2：直接下载安装

访问以下链接下载并安装：

- **.NET Framework 4.8 开发工具包**：
  https://dotnet.microsoft.com/download/dotnet-framework/net48

  或者直接下载：
  https://go.microsoft.com/fwlink/?linkid=2088517

安装完成后，重新运行构建脚本。

### 方法 3：使用命令行安装（如果已安装 Visual Studio Installer）

在 PowerShell 中运行：

```powershell
& "C:\Program Files (x86)\Microsoft Visual Studio\Installer\setup.exe" modify --installPath "C:\Program Files\Microsoft Visual Studio\2022\Community" --add Microsoft.Net.Component.4.8.SDK --quiet
```

## 验证安装

安装完成后，重新运行构建脚本：

```powershell
.\build.ps1
```

如果构建成功，会在 `Release` 文件夹中生成 `War3Trainer.exe` 文件。

