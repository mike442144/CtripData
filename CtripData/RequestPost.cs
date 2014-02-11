using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace CtripData
{
    class RequestPost
    {
        const string sUserAgent =
            "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1700.107 Safari/537.36";
        const string sContentType =
            "application/x-www-form-urlencoded";
        const string sRequestEncoding = "ascii";
        const string sResponseEncoding = "gb2312";
        /// <summary>
        /// Post data到url
        /// </summary>
        /// <param name="data">要post的数据</param>
        /// <param name="url">目标url</param>
        /// <returns>服务器响应</returns>
        public static string PostDataToUrl(string data, string url)
        {
            Encoding encoding = Encoding.GetEncoding(sRequestEncoding);
            byte[] bytesToPost = encoding.GetBytes(data);
            return PostDataToUrl(bytesToPost, url);
        }

        /// <summary>
        /// Post data到url
        /// </summary>
        /// <param name="data">要post的数据</param>
        /// <param name="url">目标url</param>
        /// <returns>服务器响应</returns>
        public static string PostDataToUrl(byte[] data, string url)
        {
            

            #region 发送post请求到服务器并读取服务器返回信息
            Stream responseStream;
            try
            {
                responseStream = PostDataToUrlW(data,url).GetResponseStream();
            }
            catch (Exception e)
            {
                // log error
                Console.WriteLine(
                    string.Format("POST操作发生异常：{0}", e.Message)
                    );
                throw e;
            }
            #endregion

            #region 读取服务器返回信息
            string stringResponse = string.Empty;
            using (StreamReader responseReader =
                new StreamReader(responseStream, Encoding.GetEncoding(sResponseEncoding)))
            {
                stringResponse = responseReader.ReadToEnd();
            }
            responseStream.Close();
            #endregion
            return stringResponse;
        }
        public static WebResponse PostDataToUrlW(string data, string url)
        {
            Encoding encoding = Encoding.GetEncoding(sRequestEncoding);
            byte[] bytesToPost = encoding.GetBytes(data);
            return PostDataToUrlW(bytesToPost, url);
        }
        public static WebResponse PostDataToUrlW(byte[] data, string url)
        {
            #region 创建httpWebRequest对象
            HttpWebRequest httpRequest = WebRequest.Create(url) as HttpWebRequest;
            if (httpRequest == null)
            {
                throw new ApplicationException(
                    string.Format("Invalid url string: {0}", url)
                    );
            }
            #endregion

            #region 填充httpWebRequest的基本信息
            httpRequest.UserAgent = sUserAgent;
            httpRequest.ContentType = sContentType;
            httpRequest.Method = "POST";
            httpRequest.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
            httpRequest.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8,en;q=0.6");
            #endregion

            #region 填充要post的内容
            httpRequest.ContentLength = data.Length;
            Stream requestStream = httpRequest.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();
            #endregion
            WebResponse result=null;
            try
            {
                result = httpRequest.GetResponse();
            }
            catch (WebException error)
            {
                Console.WriteLine(error);
            }
            return result;
        }
    }
}
