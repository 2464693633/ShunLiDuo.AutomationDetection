# 安装Prism.DryIoc包

## 当前状态

项目已配置为使用Prism.DryIoc，但需要先安装NuGet包。

## 安装步骤

### 方法1：使用NuGet包管理器（推荐）

1. 在Visual Studio中，右键点击项目
2. 选择"管理NuGet程序包"
3. 在"浏览"选项卡中搜索"Prism.DryIoc"
4. 选择版本 **8.1.97**（与Prism.Core和Prism.Wpf版本一致）
5. 点击"安装"

### 方法2：使用Package Manager Console

在Visual Studio的Package Manager Console中运行：

```powershell
Install-Package Prism.DryIoc -Version 8.1.97
```

## 依赖关系

Prism.DryIoc会自动安装以下依赖：
- Prism.Core (已安装)
- Prism.Wpf (已安装)
- DryIoc (自动安装)

## 验证安装

安装完成后：
1. 检查`packages`文件夹中是否有`Prism.DryIoc.8.1.97`文件夹
2. 检查项目引用中是否有Prism.DryIoc引用（无黄色警告）
3. 重新生成解决方案，确保没有编译错误

## 注意事项

- 确保Prism.DryIoc版本与Prism.Core和Prism.Wpf版本一致（8.1.97）
- 如果遇到版本冲突，请先卸载所有Prism相关包，然后重新安装

