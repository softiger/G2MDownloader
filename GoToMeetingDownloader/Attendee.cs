using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Websense.Utility;

namespace GotoMeetingDownloader
{
    public class Attendee
    {
        public string MeetingId;
        public string AttendeeEmail;
        public string AttendeeName;
        public double Duration;
        public string[] StartTime;
        public string[] EndTime;

        private  DateTime _startTimeOrg;
        private DateTime _endTimeOrg;
        public DateTime StartTimeOrg
        {
            get
            {
                if (StartTime != null && StartTime.Length > 0)
                    _startTimeOrg = SafeConvert.ToDate(StartTime[0]);

                return _startTimeOrg;
            }
        }

        public DateTime EndTimeOrg
        {
            get
            {
                if (EndTime != null && EndTime.Length > 0)
                    _endTimeOrg = SafeConvert.ToDate(EndTime[0]);

                return _endTimeOrg;
            }
        }

        private string _startTimeUTC;
        private string _endTimeUTC;

        public string StartTimeUTC
        {
            get
            {
                if (StartTime != null && StartTime.Length > 1)
                    _startTimeUTC = StartTime[1];

                return _startTimeUTC;
            }
        }

        public string EndTimeUTC
        {
            get
            {
                if (EndTime != null && EndTime.Length > 1)
                    _endTimeUTC = EndTime[1];

                return _endTimeUTC;
            }
        }
    }
}
