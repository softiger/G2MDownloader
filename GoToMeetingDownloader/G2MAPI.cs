using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Websense.Utility;
using System.Web;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using Websense.CommonLogging;

namespace GotoMeetingDownloader
{
    public class G2MAPI
    {
        
        public static Logger _log = new Logger("G2MAPI");

        public static IList<Group> GetGroups()
        {
            IList<Group> groups = null;

            string requestUrl = ConfigUtil.GetConfigString("GetGroups");
            string responseJSON = MakeRequest(requestUrl);

            JavaScriptSerializer javaScriptSer = new JavaScriptSerializer();
            groups = javaScriptSer.Deserialize<List<Group>>(responseJSON);

            return groups;
        }

        public static IList<Organizer> GetOrganizers(string groupKey)
        {

            IList<Organizer> organizers = null;

            string requestUrl = ConfigUtil.GetConfigString("GetOrganizers");
            requestUrl = string.Format(requestUrl, groupKey);
            string responseJSON = MakeRequest(requestUrl);

            JavaScriptSerializer javaScriptSer = new JavaScriptSerializer();
            organizers = javaScriptSer.Deserialize<List<Organizer>>(responseJSON);

            return organizers;
        }

        public static IList<Meeting> GetMeetingsByGroup(string groupKey, DateTime startDate,DateTime endDate)
        {
            IList<Meeting> meetings = null;

            string requestUrl = ConfigUtil.GetConfigString("GetMeetingsByGroup");
            requestUrl = string.Format(requestUrl, groupKey, DateTimeToUTC(startDate), DateTimeToUTC(endDate));
            string responseJSON = MakeRequest(requestUrl);

            JavaScriptSerializer javaScriptSer = new JavaScriptSerializer();
            meetings = javaScriptSer.Deserialize<List<Meeting>>(responseJSON);

            return meetings;
        }

        public  static IList<Meeting> GetMeetingsByOrganizer(string organizerKey,DateTime startDate,DateTime endDate)
        {
            IList<Meeting> meetings = null;

            string requestUrl = ConfigUtil.GetConfigString("GetMeetingsByOrganizer");
            requestUrl = string.Format(requestUrl, organizerKey, DateTimeToUTC(startDate), DateTimeToUTC(endDate));
            string responseJSON = MakeRequest(requestUrl);

            JavaScriptSerializer javaScriptSer = new JavaScriptSerializer();
            meetings = javaScriptSer.Deserialize<List<Meeting>>(responseJSON);

            return meetings;
        }

        public static IList<Attendee> GetAttendeesByGroup(string groupKey,DateTime startDate,DateTime endDate)
        {
            IList<Attendee> attendees = null;

            string requestUrl = ConfigUtil.GetConfigString("GetAttendeesByGroup");
            requestUrl = string.Format(requestUrl, groupKey, DateTimeToUTC(startDate), DateTimeToUTC(endDate));
            string responseJSON = MakeRequest(requestUrl);

            JavaScriptSerializer javaScriptSer = new JavaScriptSerializer();
            attendees = javaScriptSer.Deserialize<List<Attendee>>(responseJSON);

            return attendees;
        }

        public static IList<Attendee> GetAttendeesByOrganizer(string organizerKey, DateTime startDate, DateTime endDate)
        {
            IList<Attendee> attendees = null;

            string requestUrl = ConfigUtil.GetConfigString("GetAttendeesByOrganizer");
            requestUrl = string.Format(requestUrl, organizerKey, DateTimeToUTC(startDate), DateTimeToUTC(endDate));
            string responseJSON = MakeRequest(requestUrl);
            if (responseJSON == "[]" && ConfigUtil.GetConfigBool("RetryRequest", false))
                responseJSON = MakeRequest(requestUrl);

            JavaScriptSerializer javaScriptSer = new JavaScriptSerializer();
            attendees = javaScriptSer.Deserialize<List<Attendee>>(responseJSON);

            return attendees;
        }

        public static Meeting GetMeetingById(string meetingId)
        {
            string requestUrl = ConfigUtil.GetConfigString("GetMeeting");
            requestUrl = string.Format(requestUrl, meetingId);
            string responseJSON = MakeRequest(requestUrl);

            JavaScriptSerializer javaScriptSer = new JavaScriptSerializer();
            IList<Meeting> meetings = javaScriptSer.Deserialize<IList<Meeting>>(responseJSON);
            if (meetings != null && meetings.Count > 0)
                return meetings[0];

            return null;
        }

        public static IList<Meeting> GetMeetings(DateTime startDate,DateTime endDate)
        {
            IList<Meeting> meetings = null;

            string requestUrl = ConfigUtil.GetConfigString("GetMeetings");
            requestUrl = string.Format(requestUrl, DateTimeToUTC(startDate),DateTimeToUTC(endDate));
            string responseJSON = MakeRequest(requestUrl);

            JavaScriptSerializer javaScriptSer = new JavaScriptSerializer();
            meetings = javaScriptSer.Deserialize<IList<Meeting>>(responseJSON);

            return meetings;
        }

        public static Organizer GetOrganizer(string organizerKey)
        {
            string requestUrl = ConfigUtil.GetConfigString("GetOrganizer");
            requestUrl = string.Format(requestUrl, organizerKey);
            string responseJSON = MakeRequest(requestUrl);

            JavaScriptSerializer javaScriptSer = new JavaScriptSerializer();
            IList<Organizer> orgs = javaScriptSer.Deserialize<IList<Organizer>>(responseJSON);

            if (orgs != null && orgs.Count > 0)
                return orgs[0];

            return null;
        }

        public static Organizer GetOrganizerByEmail(string email)
        {
            string requestUrl = ConfigUtil.GetConfigString("GetOrganizerByEmail");
            requestUrl = string.Format(requestUrl, email);
            string responseJSON = MakeRequest(requestUrl);

            JavaScriptSerializer javaScriptSer = new JavaScriptSerializer();
            IList<Organizer> orgs = javaScriptSer.Deserialize <IList<Organizer>>(responseJSON);

            if (orgs != null && orgs.Count > 0)
                return orgs[0];

            return null;
        }

        /// <summary>
        /// status: active or suspended
        /// </summary>
        /// <param name="organizerKey"></param>
        /// <param name="status"></param>
        public static void UpdateOrganizer(int organizerKey, string status)
        {
            string requestUrl = ConfigUtil.GetConfigString("UpdateOrganizer");
            requestUrl = string.Format(requestUrl, organizerKey);

            string putData = "{" + string.Format("\"status\":\"{0}\"", status) + "}";
            MakePutRequest(requestUrl, putData);
        }

        private static void AddAddressField(ref string queryAddress, string addressField)
        {
            if (addressField != null && addressField.Trim() != "")
            {
                if (queryAddress != "")
                    queryAddress += ",";

                queryAddress += HttpUtility.UrlEncode(addressField);
            }
        }

        private static string GetAccessToken()
        {
            return ConfigUtil.GetConfigString("Access_Token");
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

        private static void MakePutRequest(string requestUrl,string putData)
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
