using CQSS.Pay.Model;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.DAL
{
    public class PayAppDAL
    {
        /// <summary>
        /// 获取应用方的AppSecret
        /// </summary>
        /// <param name="appId">应用方ID</param>
        /// <returns></returns>
        public static string GetAppSecret(string appId)
        {
            string sql = "SELECT AppSecret FROM Pay_App WHERE AppId=@appId AND Status=@status";
            return DbHelper.QueryScalar<string>(sql, new { appId = appId, status = (int)AppEnum.GlobalStatus.Valid });
        }
    }
}
