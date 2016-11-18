using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model
{
    /// <summary>
    /// 支付成功的通知记录
    /// </summary>
    public class NotifyBackInfo
    {
        /// <summary>
        /// 系统自增主键
        /// </summary>
        public int SysNo { get; set; }
        /// <summary>
        /// 支付结果主键
        /// </summary>
        public int ResultSysNo { get; set; }
        /// <summary>
        /// 通知结果
        /// </summary>
        public int Status { get; set; }
        /// <summary>
        /// 通知结果描述
        /// </summary>
        public string Msg { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 响应报文
        /// </summary>
        public string ResponseData { get; set; }
    }
}
