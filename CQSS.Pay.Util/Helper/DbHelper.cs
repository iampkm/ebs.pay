using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace CQSS.Pay.Util.Helper
{
    /// <summary>
    /// 数据操作类
    /// </summary>
    public class DbHelper
    {
        #region 私有字段
        /// <summary>
        /// 默认数据库连接配置名称
        /// </summary>
        private const string DEFAULT_DATABASE_CONFIG = "DefaultDatabase";
        /// <summary>
        /// 默认的数据库连接字符串
        /// </summary>
        private static string _defaultConnectionString;
        #endregion

        #region 异常日志
        /// <summary>
        /// 日志文件夹
        /// </summary>
        private const string LOG_DIRECTORY = "DbHelperError";

        /// <summary>
        /// 日志格式化字符串
        /// </summary>
        private const string LOG_FORMAT = @"DbHelper执行异常：{0}sqlStr：{1}{0}params：{2}{0}异常描述：{3}{0}异常位置：{4}";

        /// <summary>
        /// 记录异常日志
        /// </summary>
        /// <param name="ex">异常</param>
        /// <param name="sqlStr">被执行的SQL语句</param>
        /// <param name="param">执行参数</param>
        public static void WriteLog(Exception ex, string sqlStr, object param = null)
        {
            string paramStr = string.Empty;
            try
            {
                if (param is IList<IParameter>)//IParameter类型的参数
                {
                    var paramList = (IList<IParameter>)param;
                    foreach (var item in paramList)
                    {
                        paramStr += BuildParamStringInLog(item.ParamName, item.ParamValue);
                    }
                }
                else if (param is IDictionary<string, object>)//dynamic类型的参数
                {
                    var paramDict = (IDictionary<string, object>)param;
                    foreach (var item in paramDict)
                    {
                        paramStr += BuildParamStringInLog(item.Key, item.Value);
                    }
                }
                else //匿名类或其他类型的参数
                {
                    paramStr += JsonHelper.Serialize(param);
                }
            }
            catch (Exception ex2)
            {
                paramStr = "解析参数发生异常(" + ex2.Message + ")";
            }
            LogWriter.WriteLog(string.Format(LOG_FORMAT, Environment.NewLine, sqlStr, paramStr, ex.Message, ex.StackTrace), LOG_DIRECTORY, ExceptionHelper.ExceptionLevel.Exception);
        }

        /// <summary>
        /// 组装异常日志中的参数字符串
        /// </summary>
        /// <param name="paramName">参数名</param>
        /// <param name="paramValue">参数值</param>
        /// <returns></returns>
        private static string BuildParamStringInLog(string paramName, object paramValue)
        {
            string value = string.Empty;
            if (paramValue != null)
            {
                if (paramValue is byte[])
                    value = string.Format("[{0}]", string.Join(",", (byte[])paramValue));
                else
                    value = paramValue.ToString();
            }
            return string.Format("{2}{0}={1}", paramName, value, Environment.NewLine);
        }
        #endregion

        #region  执行SQL方法

        #region ExecuteSqlClient
        /// <summary>
        /// 执行SQL语句，返回影响的记录行数
        /// </summary>
        /// <param name="sqlStr">被执行的SQL语句</param>
        /// <param name="parameterList">输入参数列表</param>
        /// <param name="timeOut">超时时间(单位:秒)</param>
        /// <param name="connectionName">数据库连接</param>
        /// <param name="catchException">是否捕获异常</param>
        /// <returns>影响的记录行数</returns>
        public static int ExecuteNonQuery(string sqlStr, List<InputParameter> parameterList = null, int timeOut = 0, string connectionName = null,
            bool catchException = false)
        {
            try
            {
                using (IDbConnection connection = OpenConnection(connectionName))
                {
                    IDbCommand cmd = CreateDbCommand(connection, sqlStr, parameterList, timeOut);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (System.Exception ex)
            {
                WriteLog(ex, sqlStr, parameterList);
                if (!catchException)
                {
                    throw;
                }
                return -1;
            }
        }

        /// <summary>
        /// 执行SQL语句，返回第一行第一列结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlStr">被执行的SQL语句</param>
        /// <param name="parameterList">输入参数列表</param>
        /// <param name="timeOut">超时时间(单位:秒)</param>
        /// <param name="connectionName">数据库连接</param>
        /// <param name="catchException">是否捕获异常</param>
        /// <returns></returns>
        public static T ExecuteScalar<T>(string sqlStr, List<InputParameter> parameterList = null, int timeOut = 0, string connectionName = null,
            bool catchException = false)
        {
            try
            {
                using (IDbConnection connection = OpenConnection(connectionName))
                {
                    IDbCommand cmd = CreateDbCommand(connection, sqlStr, parameterList, timeOut);
                    object value = cmd.ExecuteScalar();
                    return Parse<T>(value);
                }
            }
            catch (System.Exception ex)
            {
                WriteLog(ex, sqlStr, parameterList);
                if (!catchException)
                {
                    throw;
                }
                return default(T);
            }
        }

        /// <summary>
        /// 执行SQL返回DataSet
        /// </summary>
        /// <param name="sqlStr">被执行的SQL语句</param>
        /// <param name="parameterList">输入参数列表</param>
        /// <param name="timeOut">超时时间(单位:秒)</param>
        /// <param name="connectionName">数据库连接</param>
        /// <param name="catchException">是否捕获异常</param>
        /// <returns>返回执行结果DataSet</returns>
        public static DataSet ExecuteDataSet(string sqlStr, List<InputParameter> parameterList = null, int timeOut = 0, string connectionName = null,
            bool catchException = false)
        {
            try
            {
                using (IDbConnection connection = OpenConnection(connectionName))
                {
                    IDbCommand cmd = CreateDbCommand(connection, sqlStr, parameterList, timeOut);
                    return AdapterFillDataSet(cmd);
                }
            }
            catch (System.Exception ex)
            {
                WriteLog(ex, sqlStr, parameterList);
                if (!catchException)
                {
                    throw;
                }
                return null;
            }
        }

        /// <summary>
        /// 执行存储过程，返回输出参数
        /// </summary>
        /// <param name="procedureName">存储过程名称</param>
        /// <param name="outputParameterList">输出参数列表</param>
        /// <param name="inputParameterList">输入参数列表</param>
        /// <param name="timeOut">超时时间(单位:秒)</param>
        /// <param name="connectionName">数据库连接</param>
        /// <param name="catchException">是否捕获异常</param>
        public static DataSet ExecuteProcedure(string procedureName, List<InputParameter> inputParameterList = null, int timeOut = 0, string connectionName = null,
             bool catchException = false)
        {
            try
            {
                using (IDbConnection connection = OpenConnection(connectionName))
                {
                    IDbCommand cmd = CreateDbCommand(connection, procedureName, inputParameterList, timeOut, CommandType.StoredProcedure);
                    return AdapterFillDataSet(cmd);
                }
            }
            catch (System.Exception ex)
            {
                WriteLog(ex, procedureName, inputParameterList);
                if (!catchException)
                {
                    throw;
                }
                return null;
            }
        }

        /// <summary>
        /// 执行存储过程，返回输出参数
        /// </summary>
        /// <param name="procedureName">存储过程名称</param>
        /// <param name="outputParameterList">输出参数列表</param>
        /// <param name="inputParameterList">输入参数列表</param>
        /// <param name="timeOut">超时时间(单位:秒)</param>
        /// <param name="connectionName">数据库连接</param>
        /// <param name="catchException">是否捕获异常</param>
        public static void ExecuteProcedure(string procedureName, List<OutputParameter> outputParameterList, List<InputParameter> inputParameterList = null,
            int timeOut = 0, string connectionName = null, bool catchException = false)
        {
            try
            {
                using (IDbConnection connection = OpenConnection(connectionName))
                {
                    DataSet ds = new DataSet();
                    IDbCommand cmd = CreateDbCommand(connection, procedureName, inputParameterList, timeOut, CommandType.StoredProcedure);
                    BuildOutputParameter(cmd, outputParameterList);
                    cmd.ExecuteNonQuery();
                    foreach (var parameter in outputParameterList)
                    {
                        var outputParameter = (IDataParameter)cmd.Parameters[parameter.ParamName];
                        parameter.ParamValue = Convert.ChangeType(outputParameter.Value, parameter.ValueType);
                    }
                }
            }
            catch (System.Exception ex)
            {
                IList<IParameter> paramList = new List<IParameter>();
                foreach (var item in inputParameterList) paramList.Add(item);
                foreach (var item in outputParameterList) paramList.Add(item);
                WriteLog(ex, procedureName, paramList);
                if (!catchException)
                {
                    throw;
                }
            }
        }
        #endregion

        #region ExecuteSQLMapper
        /// <summary>
        /// 执行SQL语句，返回影响的记录行数
        /// </summary>
        /// <param name="sql">被执行的SQL语句</param>
        /// <param name="param">参数列表</param>
        /// <param name="commandTimeout">超时时间(单位:秒)</param>
        /// <param name="commandType">命令字符串类型</param>
        /// <param name="connectionName">数据库连接</param>
        /// <param name="catchException">是否捕获异常</param>
        /// <returns>影响的记录行数</returns>
        public static int Execute(string sql, object param = null, int? commandTimeout = null, CommandType? commandType = null, string connectionName = null,
            bool catchException = false)
        {
            try
            {
                using (var connection = OpenConnection(connectionName))
                {
                    return connection.Execute(sql, param, null, commandTimeout, commandType);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex, sql, param);
                if (!catchException)
                {
                    throw;
                }
                return -1;
            }
        }

        /// <summary>
        /// 执行SQL返回泛型列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">被执行的SQL语句</param>
        /// <param name="param">参数列表</param>
        /// <param name="commandTimeout">超时时间(单位:秒)</param>
        /// <param name="buffered">数据是否缓冲</param>
        /// <param name="commandType">命令字符串类型</param>
        /// <param name="connectionName">数据库连接</param>
        /// <param name="catchException">是否捕获异常</param>
        /// <returns></returns>
        public static List<T> Query<T>(string sql, object param = null, int? commandTimeout = null, bool buffered = true, CommandType? commandType = null,
            string connectionName = null, bool catchException = false)
        {
            try
            {
                using (var connection = OpenConnection(connectionName))
                {
                    var result = connection.Query<T>(sql, param, null, buffered, commandTimeout, commandType);
                    if (result != null && result.Count() > 0)
                    {
                        return result.ToList();
                    }
                    else
                    {
                        return new List<T>();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex, sql, param);
                if (!catchException)
                {
                    throw;
                }
                return null;
            }
        }

        /// <summary>
        /// 执行SQL返回泛型对象（无结果返回初始化实体对象）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">被执行的SQL语句</param>
        /// <param name="param">参数列表</param>
        /// <param name="commandTimeout">超时时间(单位:秒)</param>
        /// <param name="buffered">数据是否缓冲</param>
        /// <param name="commandType">命令字符串类型</param>
        /// <param name="connectionName">数据库连接</param>
        /// <param name="catchException">是否捕获异常</param>
        /// <returns></returns>
        public static T QuerySingle<T>(string sql, object param = null, bool buffered = true, int? commandTimeout = null, CommandType? commandType = null,
            string connectionName = null, bool catchException = false)
        {
            try
            {
                var result = Query<T>(sql, param, commandTimeout, buffered, commandType, connectionName);
                if (result.Count > 0)
                {
                    return result.First();
                }
                else
                {
                    return (T)Activator.CreateInstance(typeof(T));
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex, sql, param);
                if (!catchException)
                {
                    throw;
                }
                return default(T);
            }
        }

        /// <summary>
        /// 执行SQL语句，返回第一行第一列结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">被执行的SQL语句</param>
        /// <param name="param">参数列表</param>
        /// <param name="commandTimeout">超时时间(单位:秒)</param>
        /// <param name="commandType">命令字符串类型</param>
        /// <param name="connectionName">数据库连接</param>
        /// <param name="catchException">是否捕获异常</param>
        /// <returns></returns>
        public static T QueryScalar<T>(string sql, object param = null, int? commandTimeout = null, CommandType? commandType = null, string connectionName = null,
            bool catchException = false)
        {
            try
            {
                using (var connection = OpenConnection(connectionName))
                {
                    return connection.ExecuteScalar<T>(sql, param, null, commandTimeout, commandType);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex, sql, param);
                if (!catchException)
                {
                    throw;
                }
                return default(T);
            }
        }
        #endregion

        #endregion

        #region  事务执行SQL

        #region ExecuteSqlClient
        /// <summary>
        /// 执行SQL语句，返回影响的记录行数
        /// </summary>
        /// <param name="functor">事务执行器</param>
        /// <param name="sqlStr">被执行的SQL语句</param>
        /// <param name="parameterList">输入参数列表</param>
        /// <param name="timeOut">超时时间(单位:秒)</param>
        /// <returns>影响的记录行数</returns>
        public static int ExecuteNonQuery(TransFunctor functor, string sqlStr, List<InputParameter> parameterList = null, int timeOut = 0)
        {
            try
            {
                if (functor == null)
                    return ExecuteNonQuery(sqlStr, parameterList, timeOut);

                IDbCommand cmd = CreateDbCommand(functor.Connection, sqlStr, parameterList, timeOut);
                cmd.Transaction = functor.Transaction;
                return cmd.ExecuteNonQuery();
            }
            catch (System.Exception ex)
            {
                DbHelper.WriteLog(ex, sqlStr, parameterList);
                throw;
            }
        }

        /// <summary>
        /// 执行SQL语句，返回第一行第一列结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="functor">事务执行器</param>
        /// <param name="sqlStr">被执行的SQL语句</param>
        /// <param name="parameterList">输入参数列表</param>
        /// <param name="timeOut">超时时间(单位:秒)</param>
        /// <returns></returns>
        public static T ExecuteScalar<T>(TransFunctor functor, string sqlStr, List<InputParameter> parameterList = null, int timeOut = 0)
        {
            try
            {
                if (functor == null)
                    return ExecuteScalar<T>(sqlStr, parameterList, timeOut);

                IDbCommand cmd = CreateDbCommand(functor.Connection, sqlStr, parameterList, timeOut);
                cmd.Transaction = functor.Transaction;
                object value = cmd.ExecuteScalar();
                return Parse<T>(value);
            }
            catch (System.Exception ex)
            {
                DbHelper.WriteLog(ex, sqlStr, parameterList);
                throw;
            }
        }

        /// <summary>
        /// 执行SQL返回DataSet
        /// </summary>
        /// <param name="functor">事务执行器</param>
        /// <param name="sqlStr">被执行的SQL语句</param>
        /// <param name="parameterList">输入参数列表</param>
        /// <param name="timeOut">超时时间(单位:秒)</param>
        /// <returns>返回执行结果DataSet</returns>
        public static DataSet ExecuteDataSet(TransFunctor functor, string sqlStr, List<InputParameter> parameterList = null, int timeOut = 0)
        {
            try
            {
                if (functor == null)
                    return ExecuteDataSet(sqlStr, parameterList, timeOut);

                IDbCommand cmd = CreateDbCommand(functor.Connection, sqlStr, parameterList, timeOut);
                cmd.Transaction = functor.Transaction;
                return AdapterFillDataSet(cmd);
            }
            catch (System.Exception ex)
            {
                DbHelper.WriteLog(ex, sqlStr, parameterList);
                throw;
            }
        }
        #endregion

        #region ExecuteSQLMapper
        /// <summary>
        /// 执行SQL语句，返回影响的记录行数
        /// </summary>
        /// <param name="transaction">执行事务</param>
        /// <param name="sql">被执行的SQL语句</param>
        /// <param name="param">参数列表</param>
        /// <param name="commandTimeout">超时时间(单位:秒)</param>
        /// <param name="commandType">命令字符串类型</param>
        /// <param name="connectionName">数据库连接</param>
        /// <returns>影响的记录行数</returns>
        public static int Execute(IDbTransaction transaction, string sql, object param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return transaction.Connection.Execute(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                WriteLog(ex, sql, param);
                throw;
            }
        }

        /// <summary>
        /// 执行SQL返回泛型列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transaction">执行事务</param>
        /// <param name="sql">被执行的SQL语句</param>
        /// <param name="param">参数列表</param>
        /// <param name="commandTimeout">超时时间(单位:秒)</param>
        /// <param name="buffered">数据是否缓冲</param>
        /// <param name="commandType">命令字符串类型</param>
        /// <param name="connectionName">数据库连接</param>
        /// <returns></returns>
        public static List<T> Query<T>(IDbTransaction transaction, string sql, object param = null, int? commandTimeout = null, bool buffered = true,
            CommandType? commandType = null)
        {
            try
            {
                var result = transaction.Connection.Query<T>(sql, param, transaction, buffered, commandTimeout, commandType).ToList();
                if (result.Count() > 0)
                {
                    return result.ToList();
                }
                else
                {
                    return new List<T>();
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex, sql, param);
                throw;
            }
        }

        /// <summary>
        /// 执行SQL返回泛型对象（无结果返回初始化实体对象）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transaction">执行事务</param>
        /// <param name="sql">被执行的SQL语句</param>
        /// <param name="param">参数列表</param>
        /// <param name="commandTimeout">超时时间(单位:秒)</param>
        /// <param name="buffered">数据是否缓冲</param>
        /// <param name="commandType">命令字符串类型</param>
        /// <param name="connectionName">数据库连接</param>
        /// <returns></returns>
        public static T QuerySingle<T>(IDbTransaction transaction, string sql, object param = null, int? commandTimeout = null, bool buffered = true,
            CommandType? commandType = null)
        {
            try
            {
                var result = Query<T>(transaction, sql, param, commandTimeout, buffered, commandType);
                if (result.Count > 0)
                {
                    return result.First();
                }
                else
                {
                    return (T)Activator.CreateInstance(typeof(T));
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex, sql, param);
                throw;
            }
        }

        /// <summary>
        /// 执行SQL语句，返回第一行第一列结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="transaction">执行事务</param>
        /// <param name="sql">被执行的SQL语句</param>
        /// <param name="param">参数列表</param>
        /// <param name="commandTimeout">超时时间(单位:秒)</param>
        /// <param name="commandType">命令字符串类型</param>
        /// <param name="connectionName">数据库连接</param>
        /// <returns></returns>
        public static T QueryScalar<T>(IDbTransaction transaction, string sql, object param = null, int? commandTimeout = null, CommandType? commandType = null)
        {
            try
            {
                return transaction.Connection.ExecuteScalar<T>(sql, param, transaction, commandTimeout, commandType);
            }
            catch (Exception ex)
            {
                WriteLog(ex, sql, param);
                throw;
            }
        }
        #endregion

        #endregion

        #region 数据批量导入
        /// <summary>
        /// 数据批量导入到SQL Server
        /// </summary>
        /// <param name="dt">数据表(TableName的值即为批量导入的表名)</param>
        /// <param name="columnMappings">列对应关系(key：dt的列名 value：导入表的列名，大小写敏感。如果该参数为null，则认为dt的列名就是导入表的列名)</param>
        /// <param name="timeOut">超时时间(单位:秒)</param>
        /// <param name="connectionName">数据库连接</param>
        public static void ExecuteSqlBulkCopy(DataTable dt, Dictionary<string, string> columnMappings = null, int timeOut = 0, string connectionName = null)
        {
            string tableName = string.Empty;
            try
            {
                if (dt == null)
                    throw new Exception("数据表对象为null");
                if (string.IsNullOrWhiteSpace(dt.TableName))
                    throw new Exception("未指定批量导入的表名");

                tableName = dt.TableName;
                using (var bulkCopy = new SqlBulkCopy(GetConnectionString(connectionName)))
                {
                    bulkCopy.DestinationTableName = dt.TableName;
                    if (columnMappings == null)
                    {
                        columnMappings = new Dictionary<string, string>();
                        foreach (DataColumn item in dt.Columns)
                        {
                            columnMappings[item.ColumnName] = item.ColumnName;
                        }
                    }

                    foreach (var item in columnMappings)
                    {
                        bulkCopy.ColumnMappings.Add(item.Key, item.Value);
                    }

                    if (timeOut > 0)
                        bulkCopy.BulkCopyTimeout = timeOut;

                    bulkCopy.WriteToServer(dt);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex, "SqlBulkCopy to " + tableName);
                throw;
            }
        }
        #endregion

        #region 组装执行参数
        /// <summary>
        /// 创建并返回一个与该连接相关联的 Command 对象
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <param name="sqlStr">执行指令文本</param>
        /// <param name="parameterList">输入参数列表</param>
        /// <param name="timeOut">超时时间(单位:秒)</param>
        /// <param name="commandType">指令类型</param>
        /// <returns></returns>
        public static IDbCommand CreateDbCommand(IDbConnection connection, string sqlStr, List<InputParameter> parameterList, int timeOut, CommandType commandType = CommandType.Text)
        {
            IDbCommand cmd = connection.CreateCommand();
            cmd.CommandType = commandType;
            cmd.CommandText = sqlStr;
            BuildInputParameter(cmd, parameterList);
            if (timeOut > 0)
                cmd.CommandTimeout = timeOut;

            return cmd;
        }

        /// <summary>
        /// 组装执行参数
        /// </summary>
        /// <param name="command">执行指令</param>
        /// <param name="parameterList">输入参数列表</param>
        public static void BuildInputParameter(IDbCommand command, List<InputParameter> parameterList)
        {
            if (parameterList != null && parameterList.Count > 0)
            {
                foreach (var parameter in parameterList)
                {
                    DbType dbType = GetParameterDbType(parameter);
                    command.Parameters.Add(new SqlParameter(parameter.ParamName, parameter.ParamValue) { DbType = dbType });
                }
            }
        }

        /// <summary>
        /// 组装输出参数
        /// </summary>
        /// <param name="command">执行指令</param>
        /// <param name="parameterList">输出参数列表</param>
        public static void BuildOutputParameter(IDbCommand command, List<OutputParameter> parameterList)
        {
            if (parameterList != null && parameterList.Count > 0)
            {
                foreach (var parameter in parameterList)
                {
                    DbType dbType = GetParameterDbType(parameter);
                    var param = new SqlParameter()
                    {
                        ParameterName = parameter.ParamName,
                        DbType = dbType,
                        Direction = ParameterDirection.Output,
                        Size = parameter.ValueSize,
                    };
                    command.Parameters.Add(param);
                }
            }
        }

        /// <summary>
        /// 获取参数的数据类型
        /// </summary>
        /// <param name="parameter">参数</param>
        /// <returns></returns>
        public static DbType GetParameterDbType(IParameter parameter)
        {
            DbType dbType = DbType.Object;
            if (parameter.ValueType.Equals(typeof(string))) { dbType = DbType.String; }
            else if (parameter.ValueType.Equals(typeof(ushort))) { dbType = DbType.UInt16; }
            else if (parameter.ValueType.Equals(typeof(short))) { dbType = DbType.Int16; }
            else if (parameter.ValueType.Equals(typeof(uint))) { dbType = DbType.UInt32; }
            else if (parameter.ValueType.Equals(typeof(int))) { dbType = DbType.Int32; }
            else if (parameter.ValueType.Equals(typeof(ulong))) { dbType = DbType.UInt64; }
            else if (parameter.ValueType.Equals(typeof(long))) { dbType = DbType.Int64; }
            else if (parameter.ValueType.Equals(typeof(float))) { dbType = DbType.Single; }
            else if (parameter.ValueType.Equals(typeof(double))) { dbType = DbType.Double; }
            else if (parameter.ValueType.Equals(typeof(decimal))) { dbType = DbType.Decimal; }
            else if (parameter.ValueType.Equals(typeof(sbyte))) { dbType = DbType.SByte; }
            else if (parameter.ValueType.Equals(typeof(byte))) { dbType = DbType.Byte; }
            else if (parameter.ValueType.Equals(typeof(byte[]))) { dbType = DbType.Binary; }
            else if (parameter.ValueType.Equals(typeof(bool))) { dbType = DbType.Boolean; }
            else if (parameter.ValueType.Equals(typeof(DateTime))) { dbType = DbType.DateTime; }
            else if (parameter.ValueType.Equals(typeof(Guid))) { dbType = DbType.Guid; }
            else if (parameter.ValueType.Equals(typeof(object))) { dbType = DbType.Object; }
            return dbType;
        }

        /// <summary>
        /// 执行数据库指令，将结果填充到DataSet
        /// </summary>
        /// <param name="command">执行指令</param>
        /// <returns></returns>
        private static DataSet AdapterFillDataSet(IDbCommand command)
        {
            DataSet ds = new DataSet();
            IDbDataAdapter adapter = new SqlDataAdapter();
            adapter.SelectCommand = command;
            adapter.Fill(ds);
            return ds;
        }
        #endregion

        #region 数据库连接
        /// <summary>
        /// 获取数据库连接字符串(依次从connectionStrings节点、appSettings节点获取，只要任意一处获取成功就立即返回该连接字符串)
        /// </summary>
        /// <param name="connectionName">数据库连接</param>
        /// <returns></returns>
        public static string GetConnectionString(string connectionName = null)
        {
            string connectionString = null;
            if (string.IsNullOrWhiteSpace(connectionName))
                connectionString = GetDefaultConnectionString();
            else
                connectionString = GetConnectionStringByName(connectionName);

            if (string.IsNullOrEmpty(connectionString))
                throw new Exception(string.Format("获取数据库连接{0}失败，请确认是否已正确配置", string.IsNullOrWhiteSpace(connectionName) ? "" : "“" + connectionName + "”"));

            return connectionString;
        }

        /// <summary>
        /// 获取默认的数据库连接字符串
        /// </summary>
        /// <returns></returns>
        private static string GetDefaultConnectionString()
        {
            if (_defaultConnectionString == null)
            {
                string defaultDatabaseName = ConfigurationManager.AppSettings[DEFAULT_DATABASE_CONFIG];
                _defaultConnectionString = GetConnectionStringByName(defaultDatabaseName);
                if (_defaultConnectionString == null)
                {
                    var connectionSetting = ConfigurationManager.ConnectionStrings[1];
                    if (connectionSetting != null)
                        _defaultConnectionString = connectionSetting.ConnectionString;
                }
            }
            return _defaultConnectionString;
        }

        /// <summary>
        /// 通过名称获取数据库连接字符串
        /// </summary>
        /// <param name="connectionName">数据库连接</param>
        /// <returns></returns>
        private static string GetConnectionStringByName(string connectionName)
        {
            var connectionSetting = ConfigurationManager.ConnectionStrings[connectionName];
            if (connectionSetting != null)
                return connectionSetting.ConnectionString;

            return ConfigurationManager.AppSettings[connectionName];
        }

        /// <summary>
        /// 打开数据库连接
        /// </summary>
        /// <param name="connectionName">数据库连接</param>
        /// <returns></returns>
        public static IDbConnection OpenConnection(string connectionName = null)
        {
            string connectionString = GetConnectionString(connectionName);
            IDbConnection connection = new SqlConnection(connectionString);
            if (connection.State != ConnectionState.Open)
                connection.Open();

            return connection;
        }
        #endregion

        #region 数据类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <typeparam name="T">返回数据类型</typeparam>
        /// <param name="value">被转换的数据</param>
        /// <returns></returns>
        public static T Parse<T>(object value)
        {
            if (value == null || value is DBNull) { return default(T); }
            if (value is T) { return (T)value; }
            var type = typeof(T);
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsEnum)
            {
                if (value is float || value is double || value is decimal)
                {
                    value = Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
                }
                return (T)Enum.ToObject(type, value);
            }
            return (T)Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        #endregion
    }

    /// <summary>
    /// 参数对象接口
    /// </summary>
    public interface IParameter
    {
        #region 属性
        /// <summary>
        /// 参数在SQL语句中占位符的名称
        /// </summary>
        string ParamName { get; set; }
        /// <summary>
        /// 参数的值
        /// </summary>
        object ParamValue { get; set; }
        /// <summary>
        /// 参数的值类型
        /// </summary>
        Type ValueType { get; set; }
        #endregion
    }

    /// <summary>
    /// 输入参数对象
    /// </summary>
    public class InputParameter : IParameter
    {
        #region 属性
        /// <summary>
        /// 输入参数对象
        /// </summary>
        public InputParameter() { }
        /// <summary>
        /// SQL语句中被执行的参数对象
        /// </summary>
        /// <param name="paramName">参数在SQL语句中占位符的名称</param>
        /// <param name="paramValue">参数的值</param>
        public InputParameter(string paramName, object paramValue)
        {
            this.ParamName = paramName;
            this.ParamValue = paramValue;
        }
        /// <summary>
        /// 参数在SQL语句中占位符的名称
        /// </summary>
        public string ParamName { get; set; }
        /// <summary>
        /// 参数的值
        /// </summary>
        public object ParamValue { get; set; }
        /// <summary>
        /// 参数的值类型
        /// </summary>
        public Type ValueType
        {
            get { return this.ParamValue.GetType(); }
            set { }
        }
        #endregion
    }

    /// <summary>
    /// 输出参数对象
    /// </summary>
    public class OutputParameter : IParameter
    {
        #region 属性
        /// <summary>
        /// 输出参数对象
        /// </summary>
        public OutputParameter() { }
        /// <summary>
        /// SQL语句中被执行的参数对象
        /// </summary>
        /// <param name="paramName">参数在SQL语句中占位符的名称</param>
        /// <param name="valueSize">返回参数值的最大大小（以字节为单位）</param>
        /// <param name="valueType">返回参数类型</param>
        public OutputParameter(string paramName, int valueSize, Type valueType)
        {
            this.ParamName = paramName;
            this.ValueSize = valueSize;
            this.ValueType = valueType;
        }
        /// <summary>
        /// 参数在SQL语句中占位符的名称
        /// </summary>
        public string ParamName { get; set; }
        /// <summary>
        /// 参数的值
        /// </summary>
        public object ParamValue { get; set; }
        /// <summary>
        /// 参数值的最大大小（以字节为单位）
        /// </summary>
        public int ValueSize { get; set; }
        /// <summary>
        /// 参数类型
        /// </summary>
        public Type ValueType { get; set; }
        #endregion
    }

    /// <summary>
    /// 事务执行器
    /// </summary>
    public class TransFunctor : IDisposable
    {
        #region 字段
        private bool _disposed;
        #endregion

        #region 构造函数
        /// <summary>
        /// 事务执行器
        /// </summary>
        /// <param name="connectionName">数据库连接</param>
        public TransFunctor(string connectionName = null)
        {
            this.Connection = DbHelper.OpenConnection(connectionName);
            this.Transaction = this.Connection.BeginTransaction();
        }
        #endregion

        #region 属性
        /// <summary>
        /// 数据库的连接
        /// </summary>
        public IDbConnection Connection { get; private set; }

        /// <summary>
        /// 事务基类
        /// </summary>
        public IDbTransaction Transaction { get; private set; }
        #endregion

        #region 公共方法
        /// <summary>
        /// 提交事务
        /// </summary>
        public void Commit()
        {
            this.Transaction.Commit();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void Rollback()
        {
            this.Transaction.Rollback();
        }
        #endregion

        #region 资源释放
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                this.Transaction.Dispose();
                this.Connection.Dispose();
                if (disposing) { }
                _disposed = true;
            }
        }
        ~TransFunctor()
        {
            this.Dispose(false);
        }
        #endregion
    }
}
