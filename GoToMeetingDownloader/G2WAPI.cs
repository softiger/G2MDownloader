using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Websense.CommonLogging;
using Websense.Utility;
using System.Web;
using System.Net;
using System.IO;

namespace GotoMeetingDownloader
{
    class G2WAPI
    {
        public static Logger _log = new Logger("G2WAPI");

        public static void GetHostoricalWebniars(int organizerKey, DateTime fromTime, DateTime toTime)
        {
            string requestUrl = ConfigUtil.GetConfigString("GetHistoricalWebinars");
            requestUrl = string.Format(requestUrl, organizerKey, DateTimeToUTC(fromTime), DateTimeToUTC(toTime));

            string responseJSON = MakeRequest(requestUrl);
        }

        public static void GetUpcomingWebniars(int organizerKey)
        {
            string requestUrl = ConfigUtil.GetConfigString("GetUpcomingWebinars");
            requestUrl = string.Format(requestUrl, organizerKey);

            string responseJSON = MakeRequest(requestUrl);
        }

        private static string GetAccessToken()
        {
            return ConfigUtil.GetConfigString("G2W_Access_Token");
        }

        private static string MakeRequest(string requestUrl)
        {

            string accessToken = GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("The access token should not be null.");

            try
            {
                LogTimerState logTimerState = null;
                if (_log.IsTimingOn) logTimerState = _log.LogTimingStart(LogType.Timing, "GotoAPI Call :" + requestUrl);
                HttpWebRequest request = CreateHttpAuthRequest(requestUrl, accessToken, WebRequestMethods.Http.Get, string.Empty);

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception(String.Format("Server error (HTTP {0}: {1}).", response.StatusCode, response.StatusDescription));

                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string result = reader.ReadToEnd();
                        if (_log.IsTimingOn) _log.LogTimingEnd(logTimerState, "GotoAPI Call :" + requestUrl);
                        return result;
                    }
                }


            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                _log.Log(ex);
                if (ex.Response != null)
                {
                    HttpWebResponse errorResponse = (HttpWebResponse)ex.Response;
                    StreamReader reader = new StreamReader(errorResponse.GetResponseStream());
                    string output = reader.ReadToEnd();
                    Console.WriteLine(string.Format("Call {0} error:\n{1}", requestUrl, output));
                    _log.Log(LogType.Audit, string.Format("Call {0} error:\n{1}", requestUrl, output));

                    DataUtil.AddErrorLog(requestUrl, ex.Message + output);
                }
                else { return "[]"; }

                return "[]";
            }
        }

        private static HttpWebRequest CreateHttpAuthRequest(string serviceEndPoint, string oAuthToken, string strMethodType, string strPostData)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(serviceEndPoint);
            request.Timeout = 100000000;
            request.ProtocolVersion = HttpVersion.Version11;
            if (strMethodType.Trim().ToUpper() == "POST")
            {
                request.Method = "POST";
            }
            else if (strMethodType.Trim().ToUpper() == "PUT")
            {
                request.Method = "PUT";
            }
            else
            {
                request.Method = "GET";
            }

            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Headers.Add(HttpRequestHeader.Authorization, oAuthToken);
            request.Proxy = new WebProxy() { UseDefaultCredentials = true };

            if (strMethodType.Trim().ToUpper() == "POST" || strMethodType.Trim().ToUpper() == "PUT")
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] body = encoding.GetBytes(strPostData);
                request.ContentLength = body.Length;
                using (Stream newStream = request.GetRequestStream())
                {
                    newStream.Write(body, 0, body.Length);
                    newStream.Close();
                }
            }

            return request;
        }

        private static void MakePutRequest(string requestUrl, string putData)
        {

            string accessToken = GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
                throw new Exception("The access token should not be null.");

            try
            {
                LogTimerState logTimerState = null;
                if (_log.IsTimingOn) logTimerState = _log.LogTimingStart(LogType.Timing, "GotoAPI Call :" + requestUrl);
                HttpWebRequest request = CreateHttpAuthRequest(requestUrl, accessToken, WebRequestMethods.Http.Put, putData);

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.NoContent)
                        throw new Exception(String.Format("Server error (HTTP {0}: {1}).", response.StatusCode, response.StatusDescription));
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine(ex.Message);
                _log.Log(ex);

                if (ex.Response != null)
                {
                    HttpWebResponse errorResponse = (HttpWebResponse)ex.Response;
                    StreamReader reader = new StreamReader(errorResponse.GetResponseStream());
                    string output = reader.ReadToEnd();
                    Console.WriteLine(string.Format("Call {0} error:\n{1}", requestUrl, output));
                    _log.Log(LogType.Audit, string.Format("Call {0} error:\n{1}", requestUrl, output));

                    DataUtil.AddErrorLog(requestUrl, ex.Message + output);
                }
            }
        }

        public static string DateTimeToUTC(DateTime date)
        {
            string datePatt = @"yyyy-MM-ddTHH:mm:ssZ";
            return date.ToString(datePatt);
        }
    }
}
