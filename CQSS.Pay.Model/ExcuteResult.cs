using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model
{
    public class ExecuteResult
    {
        public ExecuteResult()
        {
            this.Status = ResultStatus.None;
            this.Message = string.Empty;
        }

        /// <summary>
        /// 执行状态标志
        /// </summary>
        public ResultStatus Status { get; set; }

        /// <summary>
        /// 消息提示
        /// </summary>
        public string Message { get; set; }
    }

    public class ExecuteResult<T> : ExecuteResult
    {
        /// <sum mary>
        /// 数据存储
        /// </summary>
        public T Data { get; set; }
    }
    /// <summary>
    /// 执行结果
    /// </summary>
    public enum ResultStatus
    {
        /// <summary>
        /// 异常
        /// </summary>
        Error = -1,
        /// <summary>
        /// 未知
        /// </summary>
        None = 0,
        /// <summary>
        /// 成功
        /// </summary>
        Success = 1,
        /// <summary>
        /// 失败
        /// </summary>
        Failure = 2,
    }
}
