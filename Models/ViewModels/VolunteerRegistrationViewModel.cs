using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VolunteerHub.Models.ViewModels
{
    public class VolunteerRegistrationViewModel
    {
        // =========================
        // PERSONAL DETAILS
        // =========================

        [Required]
        [Display(Name = "First Name")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        [StringLength(20)]
        public string? Phone { get; set; }


        // =========================
        // ADDRESS
        // =========================

        [Required]
        [Display(Name = "Street Address")]
        [StringLength(200)]
        public string StreetAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Suburb { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Province { get; set; } = string.Empty;

        [Display(Name = "Postal Code")]
        [StringLength(10)]
        public string? PostalCode { get; set; }


        // =========================
        // ORGANIZATION
        // =========================

        [Required(ErrorMessage = "Please select an organization.")]
        [Display(Name = "Organization")]
        public int OrganizationId { get; set; }

        public IEnumerable<SelectListItem> Organizations { get; set; }
            = new List<SelectListItem>();


        // =========================
        // VOLUNTEER SKILLS
        // =========================

        [Display(Name = "Skills")]
        public List<int> SelectedSkillIds { get; set; } = new List<int>();

        public IEnumerable<SelectListItem> Skills { get; set; }
            = new List<SelectListItem>();


        // =========================
        // PASSWORD
        // =========================

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}