# 顺利多自动化检测系统

## 项目概述

顺利多自动化检测系统是一个基于WPF和Prism框架开发的桌面应用程序，用于管理自动化检测任务、设备、物流盒等资源。

## 技术栈

- **框架**: WPF (Windows Presentation Foundation)
- **.NET版本**: .NET Framework 4.8
- **MVVM框架**: Prism 7.2+
- **数据库**: SQLite
- **依赖注入**: Prism Unity容器

## 项目结构

```
顺利多自动化检测系统/
├── ShunLiDuo.AutomationDetection.sln          # 解决方案文件
│
├── ShunLiDuo.AutomationDetection.Shell/      # Shell主程序
│   ├── Views/                                # 视图
│   │   └── ShellView.xaml                    # 主Shell视图
│   ├── ViewModels/                           # 视图模型
│   │   └── ShellViewModel.cs                 # Shell视图模型
│   ├── App.xaml                              # 应用程序入口
│   └── Bootstrapper.cs                       # Prism引导程序
│
├── ShunLiDuo.AutomationDetection.Core/    # 核心业务层
│   ├── Models/                               # 实体模型
│   ├── Interfaces/                           # 接口定义
│   └── Enums/                                # 枚举定义
│
├── ShunLiDuo.AutomationDetection.Modules/    # 功能模块
│   ├── TaskManagement/                       # 任务管理模块
│   ├── LogisticsBoxManagement/               # 物流盒管理模块
│   ├── DeviceException/                      # 设备异常模块
│   └── ...                                   # 其他模块
```

## 功能模块

1. **任务管理** - 检测任务的创建、分配、执行、查询
2. **检测室管理** - 检测室信息、设备配置管理
3. **物流盒管理** - 物流盒信息、流转管理
4. **规则管理** - 检测规则配置、规则引擎
5. **角色权限** - 用户角色、权限管理
6. **设备异常** - 设备异常记录、处理
7. **设备报警** - 设备报警信息、处理流程
8. **账户管理** - 用户账户管理

## 界面特点

- **顶部Header**: 显示Logo、系统标题、用户信息和时间
- **左侧导航**: 快速访问主要功能模块
- **主内容区域**: 显示当前选中模块的内容
- **底部导航**: 底部导航栏，包含任务管理、调度规则、报警与异常、系统设置等

## 开发环境要求

- Visual Studio 2019 或更高版本
- .NET Framework 4.8
- NuGet包管理器

## 安装依赖

项目使用NuGet管理依赖包，主要依赖包括：

- Prism.Core (7.2.0.1422)
- Prism.Wpf (7.2.0.1422)
- Prism.Unity (7.2.0.1422)
- Unity.Container (5.8.6)

在Visual Studio中打开解决方案后，NuGet会自动还原这些包。

## 运行项目

1. 打开 `ShunLiDuo.AutomationDetection.sln`
2. 设置 `ShunLiDuo.AutomationDetection.Shell` 为启动项目
3. 按 F5 运行

## 模块说明

### 任务管理模块

- **扫码录入**: 输入物流盒编码和调度规则
- **检测归属**: 显示各检测室的物流盒分配情况
- **任务列表**: 显示所有检测任务的详细信息

### 物流盒管理模块

- **物流盒列表**: 显示所有物流盒信息
- **搜索功能**: 支持按编号或名称搜索
- **新增/编辑/删除**: 物流盒的增删改查功能

### 设备异常模块

- **异常列表**: 显示所有设备异常记录
- **筛选功能**: 支持按时间范围和检测室筛选
- **导出功能**: 支持导出异常数据

### 账户管理模块

- **账户列表**: 显示所有用户账户
- **账户管理**: 支持新增、编辑、删除账户
- **角色分配**: 为用户分配角色和权限

## 开发规范

- 遵循MVVM模式
- 使用Prism的依赖注入和区域管理
- 代码遵循C#编码规范
- 使用XML注释

## 后续开发

- 实现数据库连接和持久化
- 完善各模块的业务逻辑
- 添加数据验证和错误处理
- 实现报表和导出功能
- 添加单元测试

## 许可证

Copyright © 2024

