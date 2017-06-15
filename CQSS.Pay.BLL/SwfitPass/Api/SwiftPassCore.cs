using CQSS.Pay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.SwfitPass.Api
{
    public class SwiftPassCore
    {
        /// <summary>
        /// 获取默认请求参数
        /// </summary>
        /// <returns></returns>
        public static ParameterData GetDefaultParameter()
        {
            var data = new ParameterData();
            data.SetValue("version", "2.0");//接口版本号
            data.SetValue("charset", "UTF-8");//字符集
            data.SetValue("sign_type", "MD5");//签名方式
            data.SetValue("mch_id", SwiftPassConfig.mch_id);//必填项，商户号，由平台分配
            data.SetValue("mch_create_ip", System.Web.HttpContext.Current.Request.UserHostAddress);//终端IP
            data.SetValue("nonce_str", Guid.NewGuid().ToString().Replace("-", ""));//随机字符串，必填项，不长于 32 位
            return data;
        }

        /// <summary>
        /// 创建签名
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string CreateSign(ParameterData data)
        {
            StringBuilder sb = new StringBuilder();
            SortedDictionary<string, string> parameters = data.GetValues();
            foreach (KeyValuePair<string, string> item in parameters)
            {
                string k = item.Key;
                string v = item.Value;
                if (null != v && "".CompareTo(v) != 0 && "sign".CompareTo(k) != 0 && "key".CompareTo(k) != 0)
                {
                    sb.Append(k + "=" + v + "&");
                }
            }
            sb.Append("key=" + SwiftPassConfig.key);
            string sign = MD5Util.GetMD5(sb.ToString(), SwiftPassConfig.charset).ToUpper();
            return sign;
        }

        /// <summary>
        /// 校验签名
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool CheckSign(ParameterData data)
        {
            string sign = CreateSign(data).ToUpper();
            return data.GetValue("sign").ToUpper().Equals(sign);
        }
    }
}
