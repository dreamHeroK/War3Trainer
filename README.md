# War3Trainer(WarCraft III Trainer)

这是我[blog](http://tctianchi.duapp.com/)文章中关于[魔兽3内存修改器](http://tctianchi.duapp.com/archives/tag/%E9%AD%94%E5%85%BD3)对应的代码。编译好的二进制也请到那里下载。由于是十多年前的旧物所以我也只是姑且搬运到github而已，不一定更新了。

![Screenshot](https://raw.githubusercontent.com/tctianchi/War3Trainer/master/Screenshot.png)

## 修改器简介
这款修改器可以读写游戏中的游戏资源、单位攻击力、英雄属性等，帮助你在单机游戏中获得更好的娱乐体验。

### 主要功能
* 修改游戏资源（金、木、人口等）
* 修改单位属性（HP、MP、攻击力、护甲等）
* 修改英雄属性（力量、敏捷、智力、经验值等）
* **锁定护甲功能**：一键锁定单位护甲为 2E+20，持续保持不可改变

### 使用限制
* 修改器不是作弊器，只能在单机上使用
* 不适用于网络对战，更无法在战网上胡闹
* 没有向网络发送过任何欺骗信息
* 想在宿舍里联机打RPG地图的话需要在每台电脑上做相同的修改动作才不会掉线，游戏平衡性不受影响

## 支持的游戏版本
* 1.20e（1.20.4.6074）
* 1.21a（1.21.0.6263）
* 1.21b（1.21.1.6300）
* 1.22（1.22.0.6328）
* 1.23（1.23.0.6352）
* 1.24a（1.24.0.6372）
* 1.24b（1.24.1.6374）
* 1.24c（1.24.2.6378）
* 1.24d（1.24.3.6384）
* 1.24e（1.24.4.6387）
* 1.25b（1.25.1.6397）
* 1.26（1.26.0.6401）
* 1.27a（1.27.0.52240）
* 1.28（1.28.0.7205）
* 1.28f（1.28.5.7680）

## 编译和打包

### 快速开始

1. **安装开发环境**
   - 安装 Visual Studio 2017 或更高版本（推荐 Visual Studio 2022）
   - 确保安装了 .NET Framework 4.8 开发工具包

2. **使用构建脚本打包（推荐）**
   
   在项目根目录运行：
   ```powershell
   .\build.ps1
   ```
   
   如果遇到执行策略限制：
   ```powershell
   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
   .\build.ps1
   ```
   
   构建完成后，exe 文件会在 `Release` 文件夹中。

3. **使用 Visual Studio**
   - 打开 `War3Trainer.sln`
   - 选择 Release 配置
   - 生成 → 生成解决方案（`Ctrl+Shift+B`）
   - exe 文件位于 `War3Trainer\bin\Release\War3Trainer.exe`

### 详细打包说明

更多详细的打包方法和说明，请参考 [BUILD_GUIDE.md](BUILD_GUIDE.md) 文档。

### GitHub Release

项目已配置 GitHub Actions 自动构建和发布：

- **自动构建**: 每次推送到主分支时自动构建
- **自动发布**: 推送版本标签（如 `v1.0.0`）时自动创建 Release
- **手动触发**: 可在 Actions 页面手动触发构建和发布

详细说明请参考 [RELEASE.md](RELEASE.md) 文档。

## 新版本出现后的更新方法（程序员看这里）
如果有下一个版本的魔兽3，我肯定不会马上更新修改器的，你可以用下面的方法更改修改器的代码，达到升级的目的。

首先反汇编Game.dll。升级的关键在于GameContext.cs最后部分，找到你game.dll的版本号，将其添加为一组case语句。我的switch (ProcessVersion)有两段，所以这两个switch都需要添加case。随后，逐一找到ThisGameAddress、UnitListAddress、MoveSpeedAddress的值，而AttackAttributesOffset、HeroAttributesOffset、ItemsListOffset、MoveSpeedOffset是一组不变量，不需要修改。

1. 找到ThisGameAddress
    1. 用通用修改器找到英雄的力量，4字节整数，唯一地址
    2. 查找谁访问了这个地址，该地址所在的函数我称为DrawHeroAttributes，定义是：
        ```
        __thiscall DrawHeroAttributes(int *GameContext, int **HeroAttributes, int *AttributeBias, unsigned int *GBuffer)
        ```
    3. 这个函数很有特点，一些颜色字符串（例如" |CFF00FF00+"）的中间穿插了读取命令，其中一定有：
        1. [xxx + 94h]，这是力量
        2. [xxx + A8h]，这是敏捷
        3. 同理，Storm_578(… "%d" …)之前，必然还有一次函数调用，这是智力
    4. 这个函数的反向引用，所在函数头部跟进一个函数，将会看到常量dword_xxx引用，这个xxx就是ThisGameAddress
2. UnitListAddress
    1. 查找字符串"LOCAL_PLAYER"
    2. 引用该字符串的函数有很多，逐个看
    3. 一定会有一个函数，头部同时有"LOCAL_PLAYER"、"LOCAL_GAME"，末尾形如
        ```
       if (!dword_6FAA2FFC)
          dword_6FAA2FFC = sub_6F0074F0();
        ```
3. MoveSpeedAddress
    1. 打开修改器源代码，在GameTrainer.cs中有一行注释："… set breakpoint here …"，在此处设置断点（准确的说应该是这行注释的下面第2行，也就是if语句那里）
    2. 在游戏中选择一个单位，并在修改器中单击刷新按钮，程序会马上运行到这里中断
    3. 这是一个循环结构，tmpAddress2通常情况下是同一个数字，但是有一轮迭代时会是不同的数字，这个数字就是MoveSpeedAddress
