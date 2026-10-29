using System;

namespace VolunteerHub.Models.ViewModels
{
    public class VolunteerAttendanceViewModel
    {
        public int AttId { get; set; }

        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public DateOnly AttendanceDate { get; set; }

        public string? Status { get; set; }

        public decimal? HoursWorked { get; set; }

        public bool Verified { get; set; }

        public string? StaffName { get; set; }
    }
}