using CQSS.Pay.Model;
using CQSS.Pay.Util;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Basic
{
    public class SignManager
    {
        /// <summary>
        /// 生成签名
        /// </summary>
        /// <param name="data">生成签名的数据</param>
        /// <returns></returns>
        public static ExecuteResult<string> CreateSign(string data)
        {
            var result = new ExecuteResult<string>();
            if (string.IsNullOrWhiteSpace(data))
            {
                result.Message = "输入参数不能为空";
                result.Status = ResultStatus.Failure;
                return result;
            }

            var json = JsonHelper.Deserialize<object>(data);
            if (json == null)
            {
                result.Message = "输入参数必须为json格式";
                result.Status = ResultStatus.Failure;
                return result;
            }

            result.Data = CryptoHelper.SignEncrypt(data, AppConfig.SignKey);
            result.Status = ResultStatus.Success;
            return result;
        }

        /// <summary>
        /// 校验签名是否正确
        /// </summary>
        /// <param name="sign">签名</param>
        /// <param name="data">生成签名的数据</param>
        /// <returns></returns>
        public static ExecuteResult<bool> CheckSign(string sign, string data)
        {
            var result = new ExecuteResult<bool>();
            if (string.IsNullOrWhiteSpace(data))
            {
                result.Message = "data参数不能为空";
                result.Status = ResultStatus.Failure;
                return result;
            }

            try
            {
                var json = JsonHelper.Deserialize<object>(data);
                if (json == null) throw new Exception();
            }
            catch
            {
                result.Message = "data参数必须为json格式";
                result.Status = ResultStatus.Failure;
                return result;
            }

            result.Status = ResultStatus.Success;
            result.Data = CryptoHelper.SignEncrypt(data, AppConfig.SignKey) == sign;
            return result;
        }
    }
}
