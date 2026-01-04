# 安装 SQLite 包说明

## 当前状态

项目已配置为使用 SQLite 数据库，但需要先安装 SQLite NuGet 包。

## 安装步骤

### 方法1：使用 NuGet 包管理器（推荐）

1. 在 Visual Studio 中，右键点击项目
2. 选择"管理NuGet程序包"
3. 在"浏览"选项卡中搜索"System.Data.SQLite"
4. 选择版本 **1.0.118**（或最新稳定版本）
5. 点击"安装"

### 方法2：使用 Package Manager Console

在 Visual Studio 的 Package Manager Console 中运行：

```powershell
Install-Package System.Data.SQLite -Version 1.0.118
```

## 注意事项

- System.Data.SQLite 需要根据您的系统架构（x86/x64）选择正确的包
- 如果遇到架构不匹配的问题，可以安装 `System.Data.SQLite.Core` 包
- 安装完成后，项目会自动添加必要的引用

## 验证安装

安装完成后：
1. 检查`packages`文件夹中是否有`System.Data.SQLite.Core.1.0.118`文件夹
2. 检查项目引用中是否有 System.Data.SQLite 引用（无黄色警告）
3. 重新生成解决方案，确保没有编译错误

## 数据库位置

数据库文件将存储在：
`%AppData%\ShunLiDuo\AutomationDetection\AutomationDetection.db`

