using System.Collections.Generic;
using VolunteerHub.Models;

namespace VolunteerHub.Models.ViewModels
{
    public class StaffProjectAssignmentViewModel
    {
        public Project Project { get; set; } = null!;

        public List<Volunteer> Volunteers { get; set; } = new();

        public List<int> AssignedVolunteerIds { get; set; } = new();

        public List<int> SelectedVolunteerIds { get; set; } = new();

        public int Sid { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? UserName { get; set; }
    }
}