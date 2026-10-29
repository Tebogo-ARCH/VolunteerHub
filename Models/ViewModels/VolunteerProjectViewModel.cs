using System;

namespace VolunteerHub.Models.ViewModels
{
    public class VolunteerProjectViewModel
    {
        public int AssignId { get; set; }

        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateOnly StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public string? Status { get; set; }

        public string? Location { get; set; }

        public string? AssignmentStatus { get; set; }

        public string? OrganizationName { get; set; }

        // Organization details
        public string? OrganizationDescription { get; set; }

        public string? OrganizationEmail { get; set; }

        public string? OrganizationPhone { get; set; }

        // Organization address
        public string? OrganizationStreetAddress { get; set; }

        public string? OrganizationSuburb { get; set; }

        public string? OrganizationCity { get; set; }

        public string? OrganizationProvince { get; set; }

        public string? OrganizationPostalCode { get; set; }
    }
}