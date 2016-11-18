using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace CQSS.Pay.Util.Helper
{
    /// <summary>
    /// 基于HTTP协议操作类
    /// </summary>
    public class HttpHelper
    {
        #region 异常日志
        private static string _logDir = "HttpHelperError";
        private static string _logFormat = @"
Http请求异常！
异常描述：{0}
异常位置：{1}
请求地址：{2}
请求参数：{3}";
        /// <summary>
        /// 记录异常日志
        /// </summary>
        /// <param name="ex">异常</param>
        /// <param name="url">请求地址</param>
        /// <param name="param">请求参数</param>
        private static void WriteLog(Exception ex, string url, string param)
        {
            LogWriter.WriteLog(string.Format(_logFormat, ex.Message, ex.StackTrace, url, param), _logDir, ExceptionHelper.ExceptionLevel.Exception);
        }
        #endregion

        #region 默认配置
        /// <summary>
        /// UTF8编码
        /// </summary>
        private static Encoding _encoding = Encoding.UTF8;
        #endregion

        #region HttpPost请求
        /// <summary>
        /// HttpPost请求
        /// </summary>
        /// <param name="url">请求url地址</param>
        /// <param name="param">请求参数</param>
        /// <returns></returns>
        public static string HttpPost(string url, string param)
        {
            return HttpPost(url, param, _encoding);
        }
        /// <summary>
        /// HttpPost请求
        /// </summary>
        /// <param name="url">请求url地址</param>
        /// <param name="param">请求参数</param>
        /// <param name="encode">编码方式</param>
        /// <returns></returns>
        public static string HttpPost(string url, string param, Encoding encode)
        {
            try
            {
                byte[] data = encode.GetBytes(param);
                WebClient client = new WebClient();
                client.Credentials = CredentialCache.DefaultCredentials;
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                byte[] rsp = client.UploadData(new Uri(url), "POST", data);
                client.Dispose();
                string strRsp = HttpUtility.UrlDecode(encode.GetString(rsp));
                return strRsp;
            }
            catch (System.Exception ex)
            {
                WriteLog(ex, url, param);
            }
            return null;
        }
        #endregion

        #region HttpGet请求
        /// <summary>
        /// HttpGet请求
        /// </summary>
        /// <param name="url">请求url地址</param>
        /// <returns></returns>
        public static string HttpGet(string url)
        {
            return HttpGet(url, _encoding);
        }
        /// <summary>
        /// HttpGet请求
        /// </summary>
        /// <param name="url">请求url地址</param>
        /// <param name="encode">编码方式</param>
        /// <returns></returns>
        public static string HttpGet(string url, Encoding encode)
        {
            return HttpRequest("GET", url, encode);
        }
        #endregion

        #region HttpRequest请求
        /// <summary>
        /// HttpRequest请求
        /// </summary>
        /// <param name="method">提交类型(POST GET)</param>
        /// <param name="url">请求地址</param>
        /// <returns></returns>
        public static string HttpRequest(string method, string url)
        {
            return HttpRequest(method, url, string.Empty, _encoding, 0);
        }
        /// <summary>
        /// HttpRequest请求
        /// </summary>
        /// <param name="method">提交类型(POST GET)</param>
        /// <param name="url">请求地址</param>
        /// <param name="timeOut">超时时间(单位:毫秒)</param>
        /// <returns></returns>
        public static string HttpRequest(string method, string url, int timeOut)
        {
            return HttpRequest(method, url, string.Empty, _encoding, timeOut);
        }
        /// <summary>
        /// HttpRequest请求
        /// </summary>
        /// <param name="method">提交类型(POST GET)</param>
        /// <param name="url">请求地址</param>
        /// <param name="param">请求参数</param>
        /// <returns></returns>
        public static string HttpRequest(string method, string url, string param)
        {
            return HttpRequest(method, url, param, _encoding, 0);
        }
        /// <summary>
        /// HttpRequest请求
        /// </summary>
        /// <param name="method">提交类型(POST GET)</param>
        /// <param name="url">请求地址</param>
        /// <param name="param">请求参数</param>
        /// <param name="timeOut">超时时间(单位:毫秒)</param>
        /// <returns></returns>
        public static string HttpRequest(string method, string url, string param, int timeOut)
        {
            return HttpRequest(method, url, param, _encoding, timeOut);
        }
        /// <summary>
        /// HttpRequest请求
        /// </summary>
        /// <param name="method">提交类型(POST GET)</param>
        /// <param name="url">请求地址</param>
        /// <param name="param">请求参数</param>
        /// <param name="encode">编码方式</param>
        /// <returns></returns>
        public static string HttpRequest(string method, string url, string param, Encoding encode)
        {
            return HttpRequest(method, url, param, encode, 0);
        }
        /// <summary>
        /// HttpRequest请求
        /// </summary>
        /// <param name="method">提交类型(POST GET)</param>
        /// <param name="url">请求地址</param>
        /// <param name="encode">编码方式</param>
        /// <returns></returns>
        public static string HttpRequest(string method, string url, Encoding encode)
        {
            return HttpRequest(method, url, string.Empty, encode, 0);
        }
        /// <summary>
        /// HttpRequest请求
        /// </summary>
        /// <param name="method">提交类型(POST GET)</param>
        /// <param name="url">请求地址</param>
        /// <param name="encode">编码方式</param>
        /// <param name="timeOut">超时时间(单位:毫秒)</param>
        /// <returns></returns>
        public static string HttpRequest(string method, string url, Encoding encode, int timeOut)
        {
            return HttpRequest(method, url, string.Empty, encode, timeOut);
        }

        /// <summary>
        /// HttpRequest请求
        /// </summary>
        /// <param name="method">提交类型(POST GET)</param>
        /// <param name="url">请求地址</param>
        /// <param name="param">请求参数</param>
        /// <param name="endcode">编码方式</param>
        /// <param name="timeOut">超时时间(单位:毫秒)</param>
        /// <returns></returns>
        public static string HttpRequest(string method, string url, string param, Encoding encode, int timeOut)
        {
            try
            {
                if (method != null && (method.ToUpper() == "POST" || method.ToUpper() == "GET"))
                {
                    WebRequest request = WebRequest.Create(url);
                    request.Method = method.ToUpper();
                    if (request.Method == "POST")
                    {
                        byte[] data = encode.GetBytes(param);
                        request.ContentLength = data.Length;
                        request.ContentType = "application/x-www-form-urlencoded";
                        Stream writer = request.GetRequestStream();
                        writer.Write(data, 0, data.Length);
                        writer.Close();
                    }
                    if (timeOut > 0)
                    {
                        request.Timeout = timeOut;
                    }
                    WebResponse response = request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream(), encode);
                    string responseStr = reader.ReadToEnd();
                    reader.Close();
                    response.Close();
                    return responseStr;
                }
            }
            catch (System.Exception ex)
            {
                WriteLog(ex, url, param);
            }
            return null;
        }
        #endregion

        #region Http下载文件
        /// <summary>
        /// Http下载文件
        /// </summary>
        /// <param name="url">下载地址</param>
        /// <param name="filePath">文件保存路径</param>
        public static bool HttpDownLoadFile(string url, string filePath)
        {
            try
            {
                #region 文件下载方法一
                //WebClient client = new WebClient();
                //client.DownloadFile(new Uri(url), filePath);
                //client.Dispose();
                #endregion

                #region 文件下载方法二
                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponse();
                Stream reader = response.GetResponseStream();
                FileStream writer = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                byte[] buff = new byte[512];
                int c = 0; //实际读取的字节数
                while ((c = reader.Read(buff, 0, buff.Length)) > 0)
                {
                    writer.Write(buff, 0, c);
                }
                writer.Close();
                reader.Close();
                response.Close();
                #endregion
                return true;
            }
            catch (System.Exception ex)
            {
                WriteLog(ex, url, filePath);
            }
            return false;
        }
        #endregion

        #region Http下载网页
        /// <summary>
        /// Http下载网页
        /// </summary>
        /// <param name="url">url地址</param>
        /// <returns></returns>
        public static string HttpDownLoadHtml(string url)
        {
            return HttpDownLoadHtml(url, _encoding);
        }
        /// <summary>
        /// Http下载网页
        /// </summary>
        /// <param name="url">url地址</param>
        /// <returns></returns>
        public static string HttpDownLoadHtml(string url, Encoding encode)
        {
            try
            {
                WebClient client = new WebClient();
                byte[] data = client.DownloadData(new Uri(url));
                client.Dispose();
                string html = encode.GetString(data);
                return html;
            }
            catch (System.Exception ex)
            {
                WriteLog(ex, url, string.Empty);
            }
            return null;
        }
        #endregion
    }
}
