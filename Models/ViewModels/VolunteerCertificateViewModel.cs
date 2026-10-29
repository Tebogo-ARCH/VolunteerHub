using System;

namespace VolunteerHub.Models.ViewModels
{
    public class VolunteerCertificateViewModel
    {
        public int CertId { get; set; }

        public int VolunteerId { get; set; }

        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public int ReportId { get; set; }

        public DateOnly? IssuedDate { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}