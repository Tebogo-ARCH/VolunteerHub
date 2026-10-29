using System.ComponentModel.DataAnnotations;

namespace VolunteerHub.Models.ViewModels
{
    public class ManagerOrganizationEditViewModel
    {
        // ==========================================
        // ORGANIZATION INFORMATION
        // ==========================================

        public int Oid { get; set; }

        [Required]
        [StringLength(150)]
        public string OrgName { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }


        // ==========================================
        // ADDRESS INFORMATION
        // ==========================================

        public int AddressId { get; set; }

        [Required]
        [StringLength(200)]
        public string StreetAddress { get; set; } = null!;

        [StringLength(100)]
        public string? Suburb { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string Province { get; set; } = null!;

        [StringLength(10)]
        public string? PostalCode { get; set; }


        // ==========================================
        // MANAGER INFORMATION
        // ==========================================

        public int Mid { get; set; }

        public string? ManagerFirstName { get; set; }

        public string? ManagerLastName { get; set; }
    }
}