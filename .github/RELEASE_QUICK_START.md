# GitHub Release 快速开始

## 创建你的第一个 Release

### 最简单的方法（推荐）

1. **创建并推送标签**
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. **等待 GitHub Actions 自动完成**
   - 进入仓库的 **Actions** 页面
   - 查看工作流运行状态
   - 完成后，进入 **Releases** 页面即可看到新发布的版本

### 手动触发（如果需要）

1. 进入 **Actions** 页面
2. 选择 **Build and Release** 工作流
3. 点击 **Run workflow**
4. 输入版本号（例如：`1.0.0`）
5. 点击 **Run workflow**

## 工作流说明

### build.yml
- **触发时机**: 推送到主分支或创建 Pull Request
- **功能**: 自动构建项目，验证代码可以正常编译
- **输出**: 构建产物（Artifacts）

### release.yml
- **触发时机**: 
  - 推送版本标签（如 `v1.0.0`）
  - 手动触发工作流
- **功能**: 
  - 构建项目
  - 打包 Release 文件
  - 创建 GitHub Release
  - 上传发布包

## 版本标签格式

- ✅ 正确: `v1.0.0`, `v1.1.0`, `v2.0.0`
- ❌ 错误: `1.0.0` (缺少 v 前缀)

## 常见问题

### Q: 工作流失败了怎么办？
A: 检查 Actions 页面的错误信息，常见原因：
- .NET Framework 4.8 Developer Pack 安装失败
- MSBuild 路径找不到
- 代码编译错误

### Q: 如何修改 Release 说明？
A: 编辑 `.github/workflows/release.yml` 中的 `body` 部分

### Q: 可以添加更多文件到 Release 吗？
A: 可以，在 `Package Release` 步骤中添加更多 `Copy-Item` 命令

### Q: Release 文件在哪里？
A: 在 GitHub 仓库的 **Releases** 页面，每个 Release 都有下载链接

## 下一步

- 查看 [RELEASE.md](../RELEASE.md) 了解详细说明
- 查看 [CHANGELOG.md](../CHANGELOG.md) 了解如何维护更新日志

