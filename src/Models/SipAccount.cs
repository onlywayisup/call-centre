using System;

namespace CallCentre.Models
{
    public class SipAccount
    {
        public string InternalNumber { get; set; }
        public string DisplayName { get; set; }
        public string Settings { get; set; }
        public DateTime DateTime { get; set; }
    }
}