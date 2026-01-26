# MES接口使用说明

## 概述

本系统提供了HTTP REST API接口，供MES系统调用。API服务器在应用启动后自动运行在 `http://localhost:8081`。

## 重要提示

### 管理员权限
HttpSelfHostServer需要管理员权限才能监听HTTP端口。首次运行前需要执行以下命令（以管理员身份运行PowerShell或CMD）：

```cmd
netsh http add urlacl url=http://+:8081/ user=Everyone
```

或者以管理员身份运行应用程序。

### 端口配置
默认端口为8081，如需修改，请在 `App.xaml.cs` 中修改 `ApiHostService` 的构造函数参数。

## API接口列表

### 1. 获取所有检测记录

**请求**
```
GET http://localhost:8081/api/detectionlog
```

**响应**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "logisticsBoxCode": "物流盒编码001",
      "roomId": 1,
      "roomName": "检测室1",
      "status": "检测完成",
      "startTime": "2024-01-01 10:00:00",
      "endTime": "2024-01-01 10:30:00",
      "createTime": "2024-01-01 09:00:00",
      "remark": ""
    }
  ],
  "count": 1
}
```

### 2. 根据ID获取检测记录

**请求**
```
GET http://localhost:8081/api/detectionlog/{id}
```

**参数**
- `id`: 检测记录ID

**响应**
```json
{
  "success": true,
  "data": {
    "id": 1,
    "logisticsBoxCode": "物流盒编码001",
    "roomId": 1,
    "roomName": "检测室1",
    "status": "检测完成",
    "startTime": "2024-01-01 10:00:00",
    "endTime": "2024-01-01 10:30:00",
    "createTime": "2024-01-01 09:00:00",
    "remark": ""
  }
}
```

### 3. 根据物流盒编码查询

**请求**
```
GET http://localhost:8081/api/detectionlog/box/{boxCode}
```

**参数**
- `boxCode`: 物流盒编码（例如：物流盒编码001）

**响应**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "logisticsBoxCode": "物流盒编码001",
      "roomId": 1,
      "roomName": "检测室1",
      "status": "检测完成",
      "startTime": "2024-01-01 10:00:00",
      "endTime": "2024-01-01 10:30:00",
      "createTime": "2024-01-01 09:00:00",
      "remark": ""
    }
  ],
  "count": 1
}
```

### 4. 根据检测室ID查询

**请求**
```
GET http://localhost:8081/api/detectionlog/room/{roomId}
```

**参数**
- `roomId`: 检测室ID（1-5）

**响应**
```json
{
  "success": true,
  "data": [...],
  "count": 10
}
```

### 5. 根据状态查询

**请求**

```
GET http://localhost:8081/api/detectionlog/status/{status}
```

**参数**
- `status`: 状态（未检测、检测中、检测完成）

**响应**
```json
{
  "success": true,
  "data": [...],
  "count": 5
}
```

### 6. 创建检测记录

**请求**
```
POST http://localhost:8081/api/detectionlog
Content-Type: application/json
```

**请求体**
```json
{
  "logisticsBoxCode": "物流盒编码001",
  "roomId": 1,
  "roomName": "检测室1",
  "status": "未检测",
  "remark": "MES系统创建"
}
```

**响应**
```json
{
  "success": true,
  "message": "创建成功"
}
```

### 7. 查询任务状态

**请求**
```
GET http://localhost:8081/api/task/status/{boxCode}
```

**参数**
- `boxCode`: 物流盒编码

**响应**
```json
{
  "success": true,
  "data": {
    "boxCode": "物流盒编码001",
    "roomName": "检测室1",
    "status": "检测完成",
    "startTime": "2024-01-01 10:00:00",
    "endTime": "2024-01-01 10:30:00",
    "createTime": "2024-01-01 09:00:00"
  }
}
```

### 8. 创建任务

**请求**
```
POST http://localhost:8081/api/task/create
Content-Type: application/json
```

**请求体**
```json
{
  "boxCode": "物流盒编码001",
  "roomId": 1,
  "roomName": "检测室1",
  "remark": "MES系统创建的任务"
}
```

**响应**
```json
{
  "success": true,
  "message": "任务创建成功",
  "data": {
    "id": 1,
    "logisticsBoxCode": "物流盒编码001",
    "roomId": 1,
    "roomName": "检测室1",
    "status": "未检测",
    "createTime": "2024-01-01 09:00:00",
    "remark": "MES系统创建的任务"
  }
}
```

## 状态说明

- **未检测**: 物流盒已录入但尚未到达检测室
- **检测中**: 物流盒已在检测室进行检测
- **检测完成**: 物流盒检测完成并已返回生产线

## 错误处理

所有接口在发生错误时会返回HTTP 500状态码，响应格式：

```json
{
  "Message": "错误信息",
  "ExceptionMessage": "详细错误信息",
  "ExceptionType": "异常类型",
  "StackTrace": "堆栈跟踪"
}
```
## 注意事项

1. API服务器在应用启动后自动运行，无需手动启动
2. 所有时间格式为：`yyyy-MM-dd HH:mm:ss`

