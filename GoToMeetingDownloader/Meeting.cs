using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GotoMeetingDownloader
{
    public class Meeting
    {
        public int OrganizerKey;
        public string FirstName;
        public string LastName;
        public string Email;
        public string GroupName;
        public int MeetingInstanceKey;
        public string MeetingId;
        public string Subject;
        public string MeetingType;
        public double Duration;
        public DateTime StartTime;
        public DateTime EndTime;
        public string ConferenceCallInfo;
        public int NumAttendees;
    }
}
