﻿using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.IO;
using System.Text;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace CQSS.Pay.BLL.WeChatPay.Api
{
    /// <summary>
    /// http连接基础类，负责底层的http通信
    /// </summary>
    public class HttpService
    {

        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            //直接确认，否则打不开
            return true;
        }

        public static string Post(string xml, string url, bool isUseCert, int timeout)
        {
            System.GC.Collect();//垃圾回收，回收没有正常关闭的http连接

            string result = "";//返回结果

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            Stream reqStream = null;

            try
            {
                //设置最大连接数
                ServicePointManager.DefaultConnectionLimit = 200;
                //设置https验证方式
                if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    ServicePointManager.ServerCertificateValidationCallback =
                            new RemoteCertificateValidationCallback(CheckValidationResult);
                }

                /***************************************************************
                * 下面设置HttpWebRequest的相关属性
                * ************************************************************/
                request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "POST";
                request.Timeout = timeout * 1000;

                //设置代理服务器
                //WebProxy proxy = new WebProxy();                          //定义一个网关对象
                //proxy.Address = new Uri(WxPayConfig.PROXY_URL);              //网关服务器端口:端口
                //request.Proxy = proxy;

                //设置POST的数据类型和长度
                request.ContentType = "text/xml";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                request.ContentLength = data.Length;

                //是否使用证书
                if (isUseCert)
                {
                    string path = HttpContext.Current.Request.PhysicalApplicationPath;
                    path = Path.Combine(path, WxPayConfig.SSLCERT_PATH);
                    X509Certificate2 cert = new X509Certificate2(path, WxPayConfig.SSLCERT_PASSWORD);
                    request.ClientCertificates.Add(cert);
                    WxPayLog.Debug("WxPayApi", "PostXml used cert");
                }

                //往服务器写入数据
                reqStream = request.GetRequestStream();
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();

                //获取服务端返回
                response = (HttpWebResponse)request.GetResponse();

                //获取服务端返回数据
                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                result = sr.ReadToEnd().Trim();
                sr.Close();
            }
            catch (System.Threading.ThreadAbortException e)
            {
                WxPayLog.Error("HttpService", "Thread - caught ThreadAbortException - resetting.");
                WxPayLog.Error("HttpService", string.Format("Exception message: {0}", e.Message));
                System.Threading.Thread.ResetAbort();
            }
            catch (WebException e)
            {
                WxPayLog.Error("HttpService", string.Format("{1}{0}url : {2}{0}xml : {3}{0}timeout : {4}", Environment.NewLine, e.ToString(), url, xml, timeout));
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    WxPayLog.Error("HttpService", "StatusCode : " + ((HttpWebResponse)e.Response).StatusCode);
                    WxPayLog.Error("HttpService", "StatusDescription : " + ((HttpWebResponse)e.Response).StatusDescription);
                }
                throw;
            }
            catch (Exception e)
            {
                WxPayLog.Error("HttpService", string.Format("{1}{0}url : {2}{0}xml : {3}{0}timeout : {4}", Environment.NewLine, e.ToString(), url, xml, timeout));
                throw;
            }
            finally
            {
                //关闭连接和流
                if (response != null)
                {
                    response.Close();
                }
                if (request != null)
                {
                    request.Abort();
                }
            }
            return result;
        }

        /// <summary>
        /// 处理http GET请求，返回数据
        /// </summary>
        /// <param name="url">请求的url地址</param>
        /// <returns>http GET成功后返回的数据，失败抛WebException异常</returns>
        public static string Get(string url)
        {
            System.GC.Collect();
            string result = "";

            HttpWebRequest request = null;
            HttpWebResponse response = null;

            //请求url以获取数据
            try
            {
                //设置最大连接数
                ServicePointManager.DefaultConnectionLimit = 200;
                //设置https验证方式
                if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    ServicePointManager.ServerCertificateValidationCallback =
                            new RemoteCertificateValidationCallback(CheckValidationResult);
                }

                /***************************************************************
                * 下面设置HttpWebRequest的相关属性
                * ************************************************************/
                request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "GET";

                //设置代理
                //WebProxy proxy = new WebProxy();
                //proxy.Address = new Uri(WxPayConfig.PROXY_URL);
                //request.Proxy = proxy;

                //获取服务器返回
                response = (HttpWebResponse)request.GetResponse();

                //获取HTTP返回数据
                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                result = sr.ReadToEnd().Trim();
                sr.Close();
            }
            catch (System.Threading.ThreadAbortException e)
            {
                WxPayLog.Error("HttpService", "Thread - caught ThreadAbortException - resetting.");
                WxPayLog.Error("Exception message: {0}", e.Message);
                System.Threading.Thread.ResetAbort();
            }
            catch (WebException e)
            {
                WxPayLog.Error("HttpService", e.ToString());
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    WxPayLog.Error("HttpService", "StatusCode : " + ((HttpWebResponse)e.Response).StatusCode);
                    WxPayLog.Error("HttpService", "StatusDescription : " + ((HttpWebResponse)e.Response).StatusDescription);
                }
                throw;
            }
            catch (Exception e)
            {
                WxPayLog.Error("HttpService", e.ToString());
                throw;
            }
            finally
            {
                //关闭连接和流
                if (response != null)
                {
                    response.Close();
                }
                if (request != null)
                {
                    request.Abort();
                }
            }
            return result;
        }

        /// <summary>
        /// 判断是不是HTTP请求超时的异常
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool IsHttpTimeout(Exception e)
        {
            if (Regex.IsMatch(e.ToString(), @"System.Net.WebException:\s*操作超时", RegexOptions.IgnoreCase))
                return true;

            return false;
        }
    }
}