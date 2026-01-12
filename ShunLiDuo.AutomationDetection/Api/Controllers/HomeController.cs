using System.Web.Http;

namespace ShunLiDuo.AutomationDetection.Api.Controllers
{
    public class HomeController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get()
        {
            var apiInfo = new
            {
                message = "MES API 服务运行中",
                version = "1.0",
                endpoints = new
                {
                    detectionLog = new
                    {
                        getAll = "GET /api/detectionlog",
                        getById = "GET /api/detectionlog/{id}",
                        getByBoxCode = "GET /api/detectionlog/box/{boxCode}",
                        getByRoomId = "GET /api/detectionlog/room/{roomId}",
                        getByStatus = "GET /api/detectionlog/status/{status}",
                        create = "POST /api/detectionlog"
                    },
                    task = new
                    {
                        getStatus = "GET /api/task/status/{boxCode}",
                        create = "POST /api/task/create"
                    }
                },
                documentation = "详细文档请参考 MES接口使用说明.md"
            };
            
            return Ok(apiInfo);
        }
    }
}

