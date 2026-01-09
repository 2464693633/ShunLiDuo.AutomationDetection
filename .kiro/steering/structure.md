# 项目结构

## 解决方案组织
```
ShunLiDuo.AutomationDetection.sln          # 主解决方案文件
├── packages/                               # NuGet包（传统格式）
└── ShunLiDuo.AutomationDetection/         # 主应用程序项目
```

## 应用程序项目结构
```
ShunLiDuo.AutomationDetection/
├── App.xaml & App.xaml.cs                 # 应用程序入口点和Prism配置
├── Models/                                # 数据模型和实体
├── Views/                                 # XAML视图和代码隐藏
├── ViewModels/                            # 视图模型类（MVVM模式）
├── Services/                              # 业务逻辑和外部集成
├── Data/                                  # 数据访问层（仓储）
├── Converters/                            # 数据绑定的值转换器
├── Resources/                             # 应用程序资源和样式
├── Properties/                            # 程序集信息和设置
├── packages.config                        # NuGet包引用
└── *.csproj                              # 项目文件
```

## 命名约定

### 文件和类
- **视图**: `[功能]View.xaml` (例如: `TaskManagementView.xaml`)
- **视图模型**: `[功能]ViewModel.cs` (例如: `TaskManagementViewModel.cs`)
- **模型**: `[实体]Item.cs` (例如: `TaskItem.cs`, `LogisticsBoxItem.cs`)
- **服务**: `I[服务]Service.cs` & `[服务]Service.cs` (接口和实现)
- **仓储**: `I[实体]Repository.cs` & `[实体]Repository.cs`
- **对话框**: `Add[实体]Dialog.xaml` (例如: `AddLogisticsBoxDialog.xaml`)

### 文件夹
- 所有文件夹名称使用PascalCase
- 将相关功能分组在一起
- 在Services和Data文件夹中分离接口和实现

## 关键架构模式

### 依赖注入
- 所有服务在`App.xaml.cs`的`RegisterTypes()`方法中注册
- 使用构造函数注入依赖项
- 共享状态使用单例服务 (例如: `ICurrentUserService`, `IS7CommunicationService`)

### MVVM实现
- 视图有对应的视图模型
- 使用Prism的`BindableBase`进行属性更改通知
- 使用Prism的`DelegateCommand`实现命令
- 通过Prism的区域管理处理导航

### 数据访问
- 数据访问抽象的仓储模式
- 业务逻辑的服务层
- 使用直接ADO.NET的SQLite数据库（无ORM）
- 通过DI管理数据库上下文单例

### 视图注册
- 视图在`App.xaml.cs`中注册用于导航
- 对话框视图注册用于直接解析
- 使用与视图名称匹配的基于字符串的导航键