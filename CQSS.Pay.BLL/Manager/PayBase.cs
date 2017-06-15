using CQSS.Pay.BLL.Interface;
using CQSS.Pay.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Manager
{
    public abstract class PayBase : IPayBase
    {
        /// <summary>
        /// 校验是否是模拟模式
        /// </summary>
        /// <returns></returns>
        public bool CheckModeIsSimulate()
        {
            return AppConfig.Global.IsSimulateMode;
        }
    }
}
