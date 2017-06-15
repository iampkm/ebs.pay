using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Interface
{
    public interface IPayBase
    {
        /// <summary>
        /// 校验是否是模拟模式
        /// </summary>
        /// <returns></returns>
        bool CheckModeIsSimulate();
    }
}
