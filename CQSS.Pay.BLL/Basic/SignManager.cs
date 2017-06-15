using CQSS.Pay.BLL.Cache;
using CQSS.Pay.Model;
using CQSS.Pay.Util;
using CQSS.Pay.Util.Extension;
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
        /// <param name="appId">业务系统ID</param>
        /// <returns></returns>
        public static ExecuteResult<string> CreateSign(string appId, string data)
        {
            var result = new ExecuteResult<string>();
            if (string.IsNullOrWhiteSpace(appId))
            {
                result.Message = "appId参数不能为空";
                result.Status = ResultStatus.Failure;
                return result;
            }

            if (string.IsNullOrWhiteSpace(data))
            {
                result.Message = "data参数不能为空";
                result.Status = ResultStatus.Failure;
                return result;
            }

            if (!data.IsJsonString())
            {
                result.Message = "data参数必须为json格式";
                result.Status = ResultStatus.Failure;
                return result;
            }

            string appSecret = AppCache.GetAppSecret(appId);
            if (string.IsNullOrWhiteSpace(appSecret))
            {
                result.Message = "appId不存在";
                result.Status = ResultStatus.Failure;
                return result;
            }

            result.Data = CryptoHelper.SignEncrypt(data, appSecret);
            result.Status = ResultStatus.Success;
            return result;
        }

        /// <summary>
        /// 校验签名是否正确
        /// </summary>
        /// <param name="data">生成签名的数据</param>
        /// <param name="sign">签名</param>
        /// <param name="appId">业务系统ID</param>
        /// <returns></returns>
        public static ExecuteResult<bool> CheckSign(string appId, string sign, string data)
        {
            var checkResult = new ExecuteResult<bool>();
            var createResult = CreateSign(appId, data);
            if (createResult.Status != ResultStatus.Success)
            {
                checkResult.Message = createResult.Message;
                checkResult.Status = ResultStatus.Failure;
                return checkResult;
            }

            checkResult.Data = createResult.Data == sign;
            checkResult.Status = ResultStatus.Success;
            return checkResult;
        }
    }
}
