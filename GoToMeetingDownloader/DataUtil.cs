using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Websense.Utility;
using System.Data.SqlClient;
using System.Data;

namespace GotoMeetingDownloader
{
    public class DataUtil
    {
        public DataUtil()
        {
        }
        
        private const string GoToMeeting_DB = "GoToMeeting_DB";

        public static void AddGroup(int groupKey, int parentKey, string groupName, string status, int numOrganizers)
        {
            using (Db db = new Db(GoToMeeting_DB))
            {
                SqlParameter[] paras = new SqlParameter[]{
                Db.MakeParameter("@GroupKey", groupKey),
                Db.MakeParameter("@ParentKey", parentKey),
                Db.MakeParameter("@GroupName", groupName),
                Db.MakeParameter("@Status",status),
                Db.MakeParameter("@NumOrganizers", numOrganizers)
            };

                db.RunNonQuery("WS_GTM_AddGroup", paras);
            }
        }

        public static void AddOrganizer(int organizerKey,string firstName,string lastName,string email,string status,int groupId,string groupName,int maxNumAttendeesAllowed)
        {
            using (Db db = new Db(GoToMeeting_DB))
            {
                SqlParameter[] paras = new SqlParameter[]{
                Db.MakeParameter("@OrganizerKey", organizerKey),
                Db.MakeParameter("@FirstName", firstName),
                Db.MakeParameter("@LastName", lastName),
                Db.MakeParameter("@Email",email),
                Db.MakeParameter("@Status", status),
                Db.MakeParameter("@GroupId", groupId),
                Db.MakeParameter("@GroupName", groupName),
                Db.MakeParameter("@MaxNumAttendeesAllowed", maxNumAttendeesAllowed)
            };

                db.RunNonQuery("WS_GTM_AddOrganizer", paras);
            }
        }
       
        public static void AddMeeting(string meetingId, int organizerKey,int meetingInsanceKey,string subject,string meetingType,int duration,DateTime startTime,DateTime endTime,string conferenceCallInfo,int numAttendees)
        {
            using (Db db = new Db(GoToMeeting_DB))
            {
                SqlParameter[] paras = new SqlParameter[]{
                Db.MakeParameter("@MeetingId",meetingId),
                Db.MakeParameter("@OrganizerKey", organizerKey),
                Db.MakeParameter("@MeetingInstancekey", meetingInsanceKey),
                Db.MakeParameter("@Subject", subject),
                Db.MakeParameter("@MeetingType",meetingType),
                Db.MakeParameter("@Duration", duration),
                Db.MakeParameter("@StartTime", startTime),
                Db.MakeParameter("@EndTime", endTime),
                Db.MakeParameter("@ConferenceCallInfo",conferenceCallInfo),
                Db.MakeParameter("@NumAttendees", numAttendees)
            };

                db.RunNonQuery("WS_GTM_AddMeeting", paras);
            }
        }


        public static void AddAttendee(string meetingId, string attendeeName,string attendeeEmail,DateTime startTime,DateTime endTime, string startTimeUTC,string endTimeUTC)
        {
            using (Db db = new Db(GoToMeeting_DB))
            {
                SqlParameter[] paras = new SqlParameter[]{
                Db.MakeParameter("@MeetingId",meetingId),
                Db.MakeParameter("@AttendeeName", attendeeName),
                Db.MakeParameter("@AttendeeEmail", attendeeEmail),
                Db.MakeParameter("@StartTime", startTime),
                Db.MakeParameter("@EndTime",endTime),
                Db.MakeParameter("@StartTimeUTC", startTimeUTC),
                Db.MakeParameter("@EndTimeUTC",endTimeUTC)
            };

                db.RunNonQuery("WS_GTM_AddAttendees", paras);
            }
        }

        public static void AddErrorLog( string requestUrl ,string erroInfo)
        {
            using (Db db = new Db(GoToMeeting_DB))
            {
                SqlParameter[] paras = new SqlParameter[]{
                Db.MakeParameter("@RequestUrl",requestUrl),
                Db.MakeParameter("@ErrorInfo", erroInfo)
            };

                db.RunNonQuery("WS_GTM_AddErrorLog", paras);
            }
        }   
     
        public static List<Organizer> GetOrganizersByGroup(int groupKey)
        {
            List<Organizer> organizers = new List<Organizer>();
            DataTable dt = null;
            using (Db db = new Db(GoToMeeting_DB))
                dt = db.QueryDataTable("WS_GTM_GetOrganizersByGroup", new SqlParameter("@groupId", groupKey));

            if (dt == null || dt.Rows.Count == 0)
                return organizers;

            foreach (DataRow row in dt.Rows)
            {
                Organizer org = new Organizer();
                org.OrganizerKey = SafeConvert.ToInt(row["OrganizerKey"]);
                org.FirstName = row["FirstName"].ToString();
                org.LastName = row["LastName"].ToString();
                org.Email = row["Email"].ToString();
                org.Status = row["Status"].ToString();
                org.GroupId = SafeConvert.ToInt(row["GroupId"]);
                org.GroupName = row["GroupName"].ToString();
                org.MaxNumAttendeesAllowed = SafeConvert.ToInt(row["MaxNumAttendeesAllowed"], 0);
                organizers.Add(org);
            }

            return organizers;
        }
    }
}
