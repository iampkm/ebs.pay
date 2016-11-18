using CQSS.Pay.Model;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.DAL
{
    public class CommonExecutor
    {
        /// <summary>
        /// 执行SQL语句查询总记录数量
        /// </summary>
        /// <param name="sql">被执行的SQL语句</param>
        /// <param name="param">参数列表</param>
        /// <param name="commandTimeout">超时时间(单位:秒)</param>
        /// <param name="connectionName">数据库连接</param>
        /// <returns></returns>
        public static int GetDataCount(string sql, object param = null, int? commandTimeout = null, string connectionName = null)
        {
            sql = string.Format("SELECT COUNT(1) FROM ( {0} ) ResultDT", sql);
            return DbHelper.QueryScalar<int>(sql, param, commandTimeout, connectionName: connectionName);
        }

        /// <summary>
        /// 执行SQL语句查询数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="page">分页信息</param>
        /// <param name="sql">被执行的SQL语句</param>
        /// <param name="param">参数列表</param>
        /// <param name="commandTimeout">超时时间(单位:秒)</param>
        /// <param name="connectionName">数据库连接</param>
        /// <param name="rowIndexName">序号列名称</param>
        /// <returns></returns>
        public static List<T> GetData<T>(Pagger page, string sql, object param = null, int? commandTimeout = null, string connectionName = null, string rowIndexName = "RowIndex")
        {
            sql = string.Format("SELECT * FROM ( {0} ) AS ResultDT", sql);
            if (page != null && !page.GetTotal)
                sql += string.Format(" WHERE ResultDT.{0} BETWEEN {1} AND {2} ", rowIndexName, page.BeginIndex, page.EndIndex);

            return DbHelper.Query<T>(sql, param, commandTimeout, connectionName: connectionName);
        }
    }
}
