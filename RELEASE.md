# 发布说明

## 如何创建 GitHub Release

### 方法 1: 使用 Git 标签（推荐）

1. **更新版本号**（可选）
   - 编辑 `War3Trainer/Properties/AssemblyInfo.cs`
   - 更新 `AssemblyVersion` 和 `AssemblyFileVersion`

2. **创建并推送标签**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. **GitHub Actions 会自动**
   - 构建项目
   - 打包 Release 文件
   - 创建 GitHub Release
   - 上传发布包

### 方法 2: 手动触发工作流

1. 进入 GitHub 仓库的 **Actions** 页面
2. 选择 **Build and Release** 工作流
3. 点击 **Run workflow**
4. 输入版本号（例如：`1.0.0`）
5. 点击 **Run workflow** 按钮

### 方法 3: 手动创建 Release

1. 进入 GitHub 仓库的 **Releases** 页面
2. 点击 **Draft a new release**
3. 填写以下信息：
   - **Tag**: `v1.0.0`（创建新标签）
   - **Title**: `War3Trainer 1.0.0`
   - **Description**: 使用下面的模板
4. 上传 `Release` 文件夹中的文件（或压缩为 zip）
5. 点击 **Publish release**

## Release 说明模板

```markdown
## War3Trainer 1.0.0

### 下载
- [War3Trainer-v1.0.0.zip](下载链接)

### 主要功能
- 修改游戏资源（金、木、人口等）
- 修改单位属性（HP、MP、攻击力、护甲等）
- 修改英雄属性（力量、敏捷、智力、经验值等）
- **锁定护甲功能**：一键锁定单位护甲为 2E+20，持续保持不可改变

### 系统要求
- Windows 7 或更高版本
- .NET Framework 4.8 或更高版本

### 使用说明
1. 以管理员身份运行 War3Trainer.exe
2. 启动游戏后，点击"查找游戏"
3. 点击"刷新"加载单位列表
4. 选择要修改的单位或资源
5. 修改数值后点击"修改"应用

### 更新内容
- 添加锁定护甲功能
- 更新目标框架到 .NET Framework 4.8
- 改进构建脚本，自动打包 Release

---

**注意**: 此修改器仅用于单机游戏，不适用于网络对战。
```

## 版本号规范

建议使用 [语义化版本](https://semver.org/lang/zh-CN/)：

- **主版本号**：不兼容的 API 修改
- **次版本号**：向下兼容的功能性新增
- **修订号**：向下兼容的问题修正

示例：
- `v1.0.0` - 初始发布
- `v1.1.0` - 添加新功能（如锁定护甲）
- `v1.1.1` - 修复 bug
- `v2.0.0` - 重大更新，可能不兼容

## 本地构建 Release

如果需要本地构建并手动上传：

```powershell
# 1. 构建项目
.\build.ps1

# 2. 创建压缩包
Compress-Archive -Path "Release\*" -DestinationPath "War3Trainer-v1.0.0.zip" -Force

# 3. 上传到 GitHub Release
```

## 注意事项

1. **标签格式**: 必须使用 `v` 前缀，例如 `v1.0.0`
2. **权限**: 需要仓库的写入权限才能创建 Release
3. **GitHub Token**: Actions 使用 `GITHUB_TOKEN`，无需额外配置
4. **文件大小**: GitHub Release 单个文件限制为 2GB

