using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Web.Script.Serialization;
using Websense.Utility;
using Websense.CommonLogging;
using System.Configuration;

namespace GotoMeetingDownloader
{
    class Program
    {
        private static Logger _log = new Logger("GoToMeetingDownloader");

        static void Main(string[] args)
        {
            G2MAPI._log.LogRequestId = _log.LogRequestId;

            WriteCommonLog(LogType.Heartbeat, "GotoMeeting Sync Started.");
            LogTimerState logTimerState = null;
            if (_log.IsTimingOn) logTimerState = _log.LogTimingStart(LogType.Timing, "Process");

            int daySpan = ConfigUtil.GetConfigInt("DaySpan", 2);
            int latestDays = ConfigUtil.GetConfigInt("LatestDays", 1);
            if (daySpan < 2)
            {
                daySpan = 2;
                SetDaySpan(2);
            }
            if (latestDays > 58)
                latestDays = 58;

            DateTime endDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0);
            DateTime startDate = endDate.AddDays(-latestDays);
            endDate = endDate.AddSeconds(-1);     
            
            WriteCommonLog(LogType.Audit, string.Format("Start to sync GoToMeeting data from {0} to {1}.", startDate.ToString(), endDate.ToString()));
            try
            {
                IList<Group> groups = G2MAPI.GetGroups();
                WriteCommonLog(LogType.Audit, string.Format("Get {0} groups totally.", groups.Count));

                foreach (Group g in groups)
                {
                    //IList<Meeting> meetings = G2MAPI.GetMeetingsByGroup(g.Groupkey.ToString(), startDate, endDate);
                    //Console.WriteLine(string.Format("Get {0} meetings by group: {1}", meetings.Count, g.Groupkey));

                    ////test GetAttendees by group
                    //IList<Attendee> attendees = G2MAPI.GetAttendeesByGroup(g.Groupkey.ToString(), startDate, endDate);
                    //Console.WriteLine(string.Format("Get {0} attendees by group: {1}", attendees.Count, g.Groupkey));

                    //continue;

                    DataUtil.AddGroup(g.Groupkey, g.ParentKey, g.GroupName, g.Status, g.NumOrganizers);

                    WriteCommonLog(LogType.Debug, string.Format("-Gettng organizers for group : {0}", g.GroupName));
                    IList<Organizer> organizers = G2MAPI.GetOrganizers(g.Groupkey.ToString());
                    WriteCommonLog(LogType.Debug, string.Format("-Got {0} organizers for group : {1}", organizers.Count, g.GroupName));

                    if (organizers == null || organizers.Count == 0)
                        continue;

                    SyncOrganizers(g.Groupkey, organizers);

                    if (latestDays > 0)
                    {
                        if (latestDays > daySpan)
                        {
                            DateTime tempStartDate = startDate;
                            DateTime tempEndDate = startDate;
                            while (tempEndDate < endDate)
                            {
                                tempEndDate = tempEndDate.AddDays(daySpan);

                                if (tempEndDate > endDate)
                                    tempEndDate = endDate;

                                Console.WriteLine(string.Format("{0}---{1}", tempStartDate.ToString(), tempEndDate.ToString()));
                                SyncMeetingAndAttendees(g.Groupkey.ToString(), g.GroupName, tempStartDate, tempEndDate);

                                tempStartDate = tempEndDate;
                            }
                        }
                        else
                            SyncMeetingAndAttendees(g.Groupkey.ToString(), g.GroupName, startDate, endDate);
                    }

                    //To get current day data, based on many time tries, the time point got, 
                    //if the UTCNow is greater than 8:00AM, the API runs well and the current day data can be responsed,
                    //Otherwise, the API will throw invalid time error.
                    DateTime currentEndDate = DateTime.UtcNow;
                    DateTime currentStartDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0);
                    if (DateTime.UtcNow.Hour >= ConfigUtil.GetConfigInt("BeginUTCHour", 8))
                    {
                        Console.WriteLine(string.Format("{0}---{1}", currentStartDate.ToString(), currentEndDate.ToString()));
                        SyncMeetingAndAttendees(g.Groupkey.ToString(), g.GroupName, currentStartDate, currentEndDate);
                    }
                }

                WriteCommonLog(LogType.Audit, "Sync all GoToMeeting data completely.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                _log.Log(e);
            }

            if(latestDays > 2)
                SetLatestDays(2);
            if (_log.IsTimingOn) _log.LogTimingEnd(logTimerState, "");
        }


        private static void SyncOrganizers(int groupKey,IList<Organizer> organizers)
        {
            foreach (Organizer org in organizers)
                DataUtil.AddOrganizer(org.OrganizerKey, org.FirstName, org.LastName, org.Email, org.Status, org.GroupId, org.GroupName, org.MaxNumAttendeesAllowed);

            List<Organizer> localOrgnizers = DataUtil.GetOrganizersByGroup(groupKey);
            //Flag the organizer as deleted in local database
            Dictionary<int, Organizer> orgList = AddDict(organizers);
            foreach (Organizer org in localOrgnizers)
            {
                if (!orgList.Keys.Contains(org.OrganizerKey))
                    DataUtil.AddOrganizer(org.OrganizerKey, org.FirstName, org.LastName, org.Email, "deleted", org.GroupId, org.GroupName, org.MaxNumAttendeesAllowed);
            }
        }

        private static Dictionary<int, Organizer> AddDict(IList<Organizer> list)
        {
            Dictionary<int, Organizer> orgList = new Dictionary<int, Organizer>();
            List<Organizer> copList = new List<Organizer>();
            foreach (var org in list)
            {
                if (!orgList.Keys.Contains(org.OrganizerKey))
                    orgList.Add(org.OrganizerKey, org);
                else
                    copList.Add(org);
            }

            return orgList;
        }


        public static void SyncMeetingAndAttendees(string groupKey, string groupName, DateTime startDate, DateTime endDate)
        {
            IList<Meeting> meetings = G2MAPI.GetMeetingsByGroup(groupKey, startDate, endDate);
            WriteCommonLog(LogType.Debug, string.Format("--Get {0} meetings for group : {1}", meetings.Count, groupName));

            IList<string> orgKeys = new List<string>();
            foreach (Meeting m in meetings)
            {
                DataUtil.AddMeeting(m.MeetingId, m.OrganizerKey, m.MeetingInstanceKey, m.Subject, m.MeetingType, SafeConvert.ToInt(m.Duration), m.StartTime, m.EndTime, m.ConferenceCallInfo, m.NumAttendees);

                if (!orgKeys.Contains(m.OrganizerKey.ToString()))
                {
                    SyncAttendees(m.OrganizerKey.ToString(), startDate, endDate);
                    orgKeys.Add(m.OrganizerKey.ToString());
                }
            }
        }

        public static void SyncAttendees(string organizerKey, DateTime startDate, DateTime endDate)
        {
            IList<Attendee> attendees = G2MAPI.GetAttendeesByOrganizer(organizerKey, startDate, endDate);
            WriteCommonLog(LogType.Debug, string.Format("----Get {0} attendees for organizer : {1}", attendees.Count, organizerKey));
            
            foreach (Attendee a in attendees)
                DataUtil.AddAttendee(a.MeetingId, a.AttendeeName, a.AttendeeEmail, a.StartTimeOrg, a.EndTimeOrg, a.StartTimeUTC, a.EndTimeUTC);
        }

        public static void SetLatestDays(int latestDays)
        {
            Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            appConfig.AppSettings.Settings["LatestDays"].Value = latestDays.ToString();
            appConfig.Save();
        }

        public static void SetDaySpan(int daySpan)
        {
            Configuration appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            appConfig.AppSettings.Settings["DaySpan"].Value = daySpan.ToString();
            appConfig.Save();
        }

        private static void WriteCommonLog(LogType type, string message)
        {
            Console.WriteLine(message);

            switch(type)
            {
                case LogType.Audit:
                    if (_log.IsAuditOn)
                        _log.Log(type, message);
                    break;
                case LogType.Debug:
                    if (_log.IsDebugOn)
                        _log.Log(type, message);
                    break;
                case LogType.Timing:
                    break;
                case LogType.Heartbeat:
                    if (_log.IsHeartbeatOn)
                        _log.Log(type, message);
                    break;
            }            
        }
    }
}
