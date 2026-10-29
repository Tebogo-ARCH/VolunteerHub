using System;
using System.Collections.Generic;
using VolunteerHub.Models;

namespace VolunteerHub.Models.ViewModels
{
    public class StaffAttendanceViewModel
    {
        public List<Project> Projects { get; set; } = new List<Project>();

        public Project? SelectedProject { get; set; }

        public List<Volunteer> Volunteers { get; set; } =
            new List<Volunteer>();

        public List<int> SelectedVolunteerIds { get; set; } =
            new List<int>();

        public DateOnly AttendanceDate { get; set; }

        public string? Status { get; set; }

        public decimal? HoursWorked { get; set; }

        public int Sid { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
    }
}