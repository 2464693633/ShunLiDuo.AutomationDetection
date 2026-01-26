using System.Threading.Tasks;

namespace ShunLiDuo.AutomationDetection.Services
{
    public interface IMesDatabaseService
    {
        /// <summary>
        /// 根据送检单编号查询对应的检测室编号
        /// </summary>
        /// <param name="workOrderNo">送检单编号</param>
        /// <returns>检测室编号（如 "2"），如果未找到返回 null</returns>
        Task<string> GetRoomNumberByWorkOrderAsync(string workOrderNo);
        
        /// <summary>
        /// 测试数据库连接
        /// </summary>
        /// <returns>是否连接成功</returns>
        Task<bool> TestConnectionAsync();
        
        /// <summary>
        /// 上料扫码：根据送检单编号更新物流盒编码和状态为B
        /// 更新表: [dbo].[dt_pp_zl_sj_main], 字段: hz_code, flag
        /// </summary>
        /// <param name="workOrderNo">送检单编号 (sjcode)</param>
        /// <param name="boxCode">物流盒编码 (hz_code)</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateLoadingScanAsync(string workOrderNo, string boxCode);
        
        /// <summary>
        /// 下料扫码：根据送检单编号更新状态为D
        /// 更新表: [dbo].[dt_pp_zl_sj_main], 字段: flag
        /// </summary>
        /// <param name="workOrderNo">送检单编号 (sjcode)</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateUnloadingScanAsync(string workOrderNo);
        
        /// <summary>
        /// 简易模式扫码：根据送检单编号更新物流盒编码和状态为D（一步完成）
        /// 更新表: [dbo].[dt_pp_zl_sj_main], 字段: hz_code, flag
        /// </summary>
        /// <param name="workOrderNo">送检单编号 (sjcode)</param>
        /// <param name="boxCode">物流盒编码 (hz_code)</param>
        /// <returns>是否更新成功</returns>
        Task<bool> UpdateSimpleModeScanAsync(string workOrderNo, string boxCode);
    }
}
