using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Dependencies;
using System.Web.Http.SelfHost;
using Prism.Ioc;
using Newtonsoft.Json;
using ShunLiDuo.AutomationDetection.Views;

namespace ShunLiDuo.AutomationDetection.Services
{
    public class ApiHostService
    {
        private HttpSelfHostServer _server;
        private readonly IContainerProvider _containerProvider;
        private readonly string _baseAddress;
        private string _actualAddress; // 实际使用的地址

        public string ActualAddress => _actualAddress ?? _baseAddress;

        public ApiHostService(IContainerProvider containerProvider, string baseAddress = "http://localhost:8080")
        {
            _containerProvider = containerProvider;
            _baseAddress = baseAddress;
        }

        public void Start()
        {
            // 尝试从配置的端口开始，如果被占用则自动尝试其他端口
            int startPort = ExtractPort(_baseAddress);
            int maxAttempts = 10; // 最多尝试10个端口
            Exception lastException = null;

            for (int i = 0; i < maxAttempts; i++)
            {
                int currentPort = startPort + i;
                string currentAddress = ReplacePort(_baseAddress, currentPort);
                _actualAddress = currentAddress;

                try
                {
                    var config = new HttpSelfHostConfiguration(currentAddress);
                    
                    // 配置依赖解析器（关键！）
                    config.DependencyResolver = new DryIocDependencyResolver(_containerProvider);
                    
                    // 配置路由
                    config.Routes.MapHttpRoute(
                        name: "DefaultApi",
                        routeTemplate: "api/{controller}/{id}",
                        defaults: new { id = RouteParameter.Optional }
                    );

                    // 配置JSON格式
                    config.Formatters.JsonFormatter.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                    config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    
                    // 创建服务器
                    _server = new HttpSelfHostServer(config);
                    
                    // 尝试启动服务器
                    try
                    {
                        _server.OpenAsync().Wait();
                        
                        // 启动成功
                        string successMsg = $"API服务器已启动: {currentAddress}";
                        if (currentPort != startPort)
                        {
                            successMsg += $"\n\n注意：端口{startPort}被占用，已自动切换到端口{currentPort}";
                        }
                        
                        System.Diagnostics.Debug.WriteLine(successMsg);
                        
                        // 显示成功消息（如果端口改变了）
                        if (currentPort != startPort && System.Windows.Application.Current != null)
                        {
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                CustomMessageBox.ShowInformation(
                                    $"端口{startPort}被占用，API服务器已自动切换到端口{currentPort}\n\n当前地址: {currentAddress}",
                                    "API服务器启动成功");
                            });
                        }
                        
                        return; // 启动成功，退出
                    }
                    catch (AggregateException aggEx)
                    {
                        // 展开 AggregateException 获取真实异常
                        var innerEx = aggEx.Flatten().InnerExceptions.FirstOrDefault() ?? aggEx;
                        lastException = innerEx;
                        
                        // 如果是权限问题，不尝试其他端口，直接显示错误
                        if (IsPermissionIssue(innerEx))
                        {
                            HandleStartupException(innerEx);
                            return;
                        }
                        
                        // 如果是端口占用错误，继续尝试下一个端口
                        if (IsPortInUseException(innerEx))
                        {
                            _server?.Dispose();
                            _server = null;
                            System.Diagnostics.Debug.WriteLine($"端口{currentPort}被占用，尝试下一个端口...");
                            continue; // 继续尝试下一个端口
                        }
                        else
                        {
                            // 其他错误，停止尝试
                            HandleStartupException(innerEx);
                            return;
                        }
                    }
                }
                catch (System.Net.HttpListenerException ex)
                {
                    lastException = ex;
                    
                    // 如果是权限错误，不尝试其他端口，直接显示错误
                    if (IsPermissionIssue(ex))
                    {
                        HandleStartupException(ex);
                        return;
                    }
                    
                    // 如果是端口占用错误，继续尝试下一个端口
                    if (IsPortInUseException(ex))
                    {
                        _server?.Dispose();
                        _server = null;
                        System.Diagnostics.Debug.WriteLine($"端口{currentPort}被占用，尝试下一个端口...");
                        continue; // 继续尝试下一个端口
                    }
                    else
                    {
                        // 其他错误，停止尝试
                        HandleStartupException(ex);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    // 其他未知错误，停止尝试
                    HandleStartupException(ex);
                    return;
                }
            }

            // 所有端口都尝试失败
            if (lastException != null)
            {
                HandleStartupException(lastException, $"已尝试端口 {startPort} 到 {startPort + maxAttempts - 1}，均失败");
            }
        }

        private int ExtractPort(string address)
        {
            try
            {
                var uri = new Uri(address);
                return uri.Port;
            }
            catch
            {
                return 8080; // 默认端口
            }
        }

        private string ReplacePort(string address, int newPort)
        {
            try
            {
                var uri = new Uri(address);
                var builder = new UriBuilder(uri)
                {
                    Port = newPort
                };
                return builder.Uri.ToString().TrimEnd('/');
            }
            catch
            {
                // 如果解析失败，简单替换
                return address.Replace(":8080", $":{newPort}");
            }
        }

        private bool IsPortInUseException(Exception ex)
        {
            // 首先检查是否是权限问题（URL ACL）
            string msg = ex.Message?.ToLower() ?? "";
            if (msg.Contains("无法注册") || msg.Contains("cannot register") || 
                msg.Contains("access denied") || msg.Contains("访问被拒绝"))
            {
                // 这通常是权限问题，不是真正的端口占用
                return false;
            }
            
            if (ex is System.Net.HttpListenerException httpEx)
            {
                // 错误代码 5 是权限问题
                if (httpEx.ErrorCode == 5)
                {
                    return false; // 权限问题，不是端口占用
                }
                // 错误代码 183 或 10048 表示端口被占用
                return httpEx.ErrorCode == 183 || httpEx.ErrorCode == 10048;
            }
            
            // 检查异常消息中是否包含端口占用相关的关键词
            return msg.Contains("端口") && (msg.Contains("占用") || msg.Contains("使用") || msg.Contains("already in use"));
        }

        private bool IsPermissionIssue(Exception ex)
        {
            string msg = ex.Message?.ToLower() ?? "";
            if (msg.Contains("无法注册") || msg.Contains("cannot register") || 
                msg.Contains("access denied") || msg.Contains("访问被拒绝"))
            {
                return true;
            }
            
            if (ex is System.Net.HttpListenerException httpEx)
            {
                return httpEx.ErrorCode == 5; // 错误代码 5 是权限问题
            }
            
            return false;
        }

        private void HandleStartupException(Exception ex, string additionalInfo = null)
        {
            string errorMsg = $"API服务器启动失败: {ex.Message}\n\n";
            
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                errorMsg += additionalInfo + "\n\n";
            }
            
            // 检查是否是权限问题（URL ACL）
            string msg = ex.Message?.ToLower() ?? "";
            bool isPermissionIssue = IsPermissionIssue(ex) || 
                                    msg.Contains("无法注册") || msg.Contains("cannot register") || 
                                    msg.Contains("access denied") || msg.Contains("访问被拒绝");
            
            if (isPermissionIssue)
            {
                errorMsg += "原因：缺少URL ACL权限\n\n";
                errorMsg += "HttpSelfHostServer需要URL ACL权限才能监听HTTP端口。\n\n";
                errorMsg += "解决方案（任选其一）：\n\n";
                errorMsg += "方案1：以管理员身份运行应用程序\n\n";
                errorMsg += "方案2：以管理员身份执行以下命令（推荐）：\n\n";
                errorMsg += "    netsh http add urlacl url=http://+:8080/ user=Everyone\n\n";
                errorMsg += "   如果需要使用其他端口，请替换8080为相应端口号\n\n";
                errorMsg += "   例如：netsh http add urlacl url=http://+:8081/ user=Everyone\n\n";
                errorMsg += "执行命令后，重新启动应用程序。";
            }
            else if (ex is System.Net.HttpListenerException httpEx)
            {
                if (httpEx.ErrorCode == 183 || httpEx.ErrorCode == 10048) // 地址已在使用
                {
                    errorMsg += "原因：端口已被占用（错误代码: " + httpEx.ErrorCode + "）\n\n";
                    errorMsg += "解决方案：\n";
                    errorMsg += "1. 关闭占用端口的程序\n";
                    errorMsg += "2. 修改API端口配置\n";
                    errorMsg += "3. 检查是否有其他实例正在运行";
                }
                else
                {
                    errorMsg += "HTTP监听器错误（错误代码: " + httpEx.ErrorCode + "）";
                }
            }
            else
            {
                errorMsg += "异常类型: " + ex.GetType().Name;
                if (ex.InnerException != null)
                {
                    errorMsg += "\n内部异常: " + ex.InnerException.Message;
                }
            }
            
            System.Diagnostics.Debug.WriteLine(errorMsg);
            System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            
            // 显示错误消息（在UI线程）
            try
            {
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        CustomMessageBox.ShowError(errorMsg, "API服务器启动失败");
                    });
                }
            }
            catch
            {
                // 如果UI线程不可用，只记录日志
            }
        }

        public void Stop()
        {
            try
            {
                _server?.CloseAsync().Wait();
                _server?.Dispose();
                System.Diagnostics.Debug.WriteLine("API服务器已停止");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API服务器停止失败: {ex.Message}");
            }
        }
    }

    // 自定义依赖解析器
    public class DryIocDependencyResolver : IDependencyResolver
    {
        private readonly IContainerProvider _container;

        public DryIocDependencyResolver(IContainerProvider container)
        {
            _container = container;
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return _container.Resolve(serviceType);
            }
            catch
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                // IContainerProvider 没有 ResolveAll 方法，尝试解析单个服务
                var service = _container.Resolve(serviceType);
                if (service != null)
                {
                    return new[] { service };
                }
                return new object[0];
            }
            catch
            {
                return new object[0];
            }
        }

        public void Dispose()
        {
            // DryIoc容器由外部管理，这里不需要释放
        }
    }
}

