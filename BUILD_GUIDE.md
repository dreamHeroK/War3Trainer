# War3Trainer 打包指南

本文档说明如何将 War3Trainer 项目打包成 exe 可执行文件。

## 项目信息

- **项目类型**: C# Windows Forms 应用程序
- **目标框架**: .NET Framework 4.8
- **输出类型**: WinExe (Windows 可执行文件)

## 打包方法

### 方法 1: 使用 Visual Studio（推荐）

1. 打开 `War3Trainer.sln` 解决方案文件
2. 在顶部工具栏选择 **Release** 配置（而不是 Debug）
3. 选择菜单：**生成** → **生成解决方案**（或按 `Ctrl+Shift+B`）
4. 构建完成后，exe 文件位于：
   ```
   War3Trainer\bin\Release\War3Trainer.exe
   ```

### 方法 2: 使用构建脚本

#### Windows 批处理脚本（build.bat）

双击运行 `build.bat`，或在命令行中执行：
```bash
build.bat
```

#### PowerShell 脚本（build.ps1）

在 PowerShell 中执行：
```powershell
.\build.ps1
```

如果遇到执行策略限制，可以临时允许执行：
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\build.ps1
```

### 方法 3: 使用 MSBuild 命令行

直接使用 MSBuild 命令：

```bash
# 查找 MSBuild 路径（通常在以下位置之一）
# Visual Studio 2022:
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" War3Trainer.sln /p:Configuration=Release

# Visual Studio 2019:
"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" War3Trainer.sln /p:Configuration=Release

# 或使用 .NET Framework 自带的 MSBuild:
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" War3Trainer.sln /p:Configuration=Release
```

## 输出文件位置

### 构建输出

构建成功后，所有文件都在以下目录：
```
War3Trainer\bin\Release\
```

主要文件：
- `War3Trainer.exe` - 主程序可执行文件
- `War3Trainer.exe.config` - 配置文件（如果有）
- 其他依赖的 DLL 文件（如果有）

### 打包输出（使用 build.ps1）

如果使用 `build.ps1` 脚本构建，会自动将文件打包到：
```
Release\
```

打包后的文件夹包含：
- `War3Trainer.exe` - 主程序可执行文件
- `War3Trainer.exe.config` - 配置文件

这个 `Release` 文件夹可以直接分发，用户只需要确保安装了 .NET Framework 4.8 或更高版本即可运行。

## 系统要求

### 开发环境

- **Visual Studio 2017 或更高版本**（推荐 Visual Studio 2022）
- **.NET Framework 4.8 开发工具包**（SDK/Targeting Pack）
  - 如果构建时出现 "找不到 .NET Framework 的引用程序集" 错误，需要安装开发工具包
  - 可以通过 Visual Studio Installer 安装，或从 [Microsoft 官网](https://dotnet.microsoft.com/download/dotnet-framework/net48) 下载

### 运行环境

- **.NET Framework 4.8 或更高版本**（通常 Windows 10/11 已预装）
- **Windows 7 或更高版本**

## 注意事项

1. **.NET Framework 要求**: 目标机器需要安装 .NET Framework 4.8 或更高版本才能运行
2. **依赖项**: 如果项目引用了第三方库，需要确保这些 DLL 文件与 exe 在同一目录
3. **配置文件**: `app.config` 会在构建时自动复制为 `War3Trainer.exe.config`
4. **管理员权限**: 修改器需要管理员权限才能访问游戏进程内存

## 发布单文件（可选）

如果需要将程序打包成单个 exe 文件（包含所有依赖），可以考虑：

1. **使用 .NET Core/.NET 5+**: 迁移到新框架后可以使用单文件发布
2. **使用第三方工具**: 如 ILMerge、Costura.Fody 等（需要修改项目配置）

## 验证构建

构建完成后，可以：
1. 检查 `War3Trainer\bin\Release\` 目录是否存在 exe 文件
2. 双击运行 exe 文件测试是否正常工作
3. 检查文件大小是否合理（通常几 MB 到几十 MB）

