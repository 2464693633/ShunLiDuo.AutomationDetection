using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using ShunLiDuo.AutomationDetection.Models;
using ShunLiDuo.AutomationDetection.Services;

namespace ShunLiDuo.AutomationDetection.Api.Controllers
{
    [RoutePrefix("api/task")]
    public class TaskController : ApiController
    {
        private IDetectionLogService _detectionLogService;
        private IRuleService _ruleService;
        private IDetectionRoomService _detectionRoomService;

        public IDetectionLogService DetectionLogService
        {
            get
            {
                if (_detectionLogService == null)
                {
                    _detectionLogService = (IDetectionLogService)Request.GetDependencyScope()
                        .GetService(typeof(IDetectionLogService));
                }
                return _detectionLogService;
            }
            set { _detectionLogService = value; }
        }

        public IRuleService RuleService
        {
            get
            {
                if (_ruleService == null)
                {
                    _ruleService = (IRuleService)Request.GetDependencyScope()
                        .GetService(typeof(IRuleService));
                }
                return _ruleService;
            }
            set { _ruleService = value; }
        }

        public IDetectionRoomService DetectionRoomService
        {
            get
            {
                if (_detectionRoomService == null)
                {
                    _detectionRoomService = (IDetectionRoomService)Request.GetDependencyScope()
                        .GetService(typeof(IDetectionRoomService));
                }
                return _detectionRoomService;
            }
            set { _detectionRoomService = value; }
        }

        public TaskController()
        {
        }

        public TaskController(
            IDetectionLogService detectionLogService,
            IRuleService ruleService,
            IDetectionRoomService detectionRoomService)
        {
            _detectionLogService = detectionLogService;
            _ruleService = ruleService;
            _detectionRoomService = detectionRoomService;
        }

        // GET api/task/status/{boxCode}
        [HttpGet]
        [Route("status/{boxCode}")]
        public async Task<IHttpActionResult> GetTaskStatus(string boxCode)
        {
            try
            {
                var logs = await DetectionLogService.GetLogsByBoxCodeAsync(boxCode);
                var latestLog = logs.OrderByDescending(l => l.CreateTime).FirstOrDefault();
                
                if (latestLog == null)
                {
                    return Ok(new { success = true, message = "未找到该物流盒的任务" });
                }

                return Ok(new { 
                    success = true, 
                    data = new {
                        boxCode = latestLog.LogisticsBoxCode,
                        roomName = latestLog.RoomName,
                        status = latestLog.Status,
                        startTime = latestLog.StartTime,
                        endTime = latestLog.EndTime,
                        createTime = latestLog.CreateTime
                    }
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST api/task/create
        [HttpPost]
        [Route("create")]
        public async Task<IHttpActionResult> CreateTask([FromBody] TaskCreateRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.BoxCode))
                {
                    return BadRequest("物流盒编码不能为空");
                }

                // 创建检测日志
                var log = new DetectionLogItem
                {
                    LogisticsBoxCode = request.BoxCode,
                    RoomId = request.RoomId,
                    RoomName = request.RoomName,
                    Status = "未检测",
                    CreateTime = DateTime.Now,
                    Remark = request.Remark
                };

                var result = await DetectionLogService.AddLogAsync(log);
                
                if (result)
                {
                    return Ok(new { success = true, message = "任务创建成功", data = log });
                }
                else
                {
                    return BadRequest("任务创建失败");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }

    public class TaskCreateRequest
    {
        public string BoxCode { get; set; }
        public int? RoomId { get; set; }
        public string RoomName { get; set; }
        public string Remark { get; set; }
    }
}

