# 技术栈

## 框架和平台
- **.NET Framework 4.8** - 目标框架
- **WPF (Windows Presentation Foundation)** - 桌面UI框架
- **Prism 8.1.97** - 使用DryIoc容器的MVVM框架
- **Material Design Themes 4.9.0** - UI组件库

## 架构模式
- **MVVM (Model-View-ViewModel)** - 主要架构模式
- **依赖注入** - 使用Prism的DryIoc容器
- **仓储模式** - 数据访问抽象
- **服务层** - 业务逻辑分离

## 核心库
- **DryIoc 4.7.7** - IoC容器
- **S7netplus 0.20.0** - 西门子S7 PLC通信
- **System.Data.SQLite 1.0.118** - 本地数据库
- **Microsoft.Xaml.Behaviors.Wpf 1.1.39** - XAML行为

## 构建系统
- **MSBuild** - 标准.NET Framework构建系统
- **NuGet** - 包管理
- **Visual Studio 2017+** - 推荐IDE

## 常用命令
```cmd
# 构建解决方案
msbuild ShunLiDuo.AutomationDetection.sln /p:Configuration=Release

# 还原NuGet包
nuget restore ShunLiDuo.AutomationDetection.sln

# 构建特定项目
msbuild ShunLiDuo.AutomationDetection\ShunLiDuo.AutomationDetection.csproj

# 清理构建
msbuild ShunLiDuo.AutomationDetection.sln /t:Clean
```

## 数据库
- **SQLite** - 本地数据存储的嵌入式数据库
- **Entity Framework** - 未使用，直接使用ADO.NET与SQLite
- **仓储模式** - 数据访问抽象层