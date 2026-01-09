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
    [RoutePrefix("api/detectionlog")]
    public class DetectionLogController : ApiController
    {
        private IDetectionLogService _detectionLogService;

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

        public DetectionLogController()
        {
        }

        public DetectionLogController(IDetectionLogService detectionLogService)
        {
            _detectionLogService = detectionLogService;
        }

        // GET api/detectionlog
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetAllLogs()
        {
            try
            {
                var logs = await DetectionLogService.GetAllLogsAsync();
                return Ok(new { success = true, data = logs, count = logs.Count });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/detectionlog/{id}
        [HttpGet]
        [Route("{id}")]
        public async Task<IHttpActionResult> GetLogById(int id)
        {
            try
            {
                var log = await DetectionLogService.GetLogByIdAsync(id);
                if (log == null)
                {
                    return NotFound();
                }
                return Ok(new { success = true, data = log });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/detectionlog/box/{boxCode}
        [HttpGet]
        [Route("box/{boxCode}")]
        public async Task<IHttpActionResult> GetLogsByBoxCode(string boxCode)
        {
            try
            {
                var logs = await DetectionLogService.GetLogsByBoxCodeAsync(boxCode);
                return Ok(new { success = true, data = logs, count = logs.Count });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/detectionlog/room/{roomId}
        [HttpGet]
        [Route("room/{roomId}")]
        public async Task<IHttpActionResult> GetLogsByRoomId(int roomId)
        {
            try
            {
                var logs = await DetectionLogService.GetLogsByRoomIdAsync(roomId);
                return Ok(new { success = true, data = logs, count = logs.Count });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET api/detectionlog/status/{status}
        [HttpGet]
        [Route("status/{status}")]
        public async Task<IHttpActionResult> GetLogsByStatus(string status)
        {
            try
            {
                var allLogs = await DetectionLogService.GetAllLogsAsync();
                var logs = allLogs.Where(l => l.Status == status).ToList();
                return Ok(new { success = true, data = logs, count = logs.Count });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // POST api/detectionlog
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> CreateLog([FromBody] DetectionLogItem log)
        {
            try
            {
                if (log == null)
                {
                    return BadRequest("日志数据不能为空");
                }

                log.CreateTime = DateTime.Now;
                var result = await DetectionLogService.AddLogAsync(log);
                
                if (result)
                {
                    return Ok(new { success = true, message = "创建成功" });
                }
                else
                {
                    return BadRequest("创建失败");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}

