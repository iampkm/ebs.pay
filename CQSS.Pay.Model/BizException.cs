using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model
{
    public class BizException : Exception
    {
        public BizException(string msg)
            : base(msg)
        {

        }
    }
}
