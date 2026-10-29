using System.ComponentModel.DataAnnotations;

namespace VolunteerHub.Models.ViewModels
{
    public class VolunteerProfileViewModel
    {
        // ==========================================
        // VOLUNTEER INFORMATION
        // ==========================================

        public int Vid { get; set; }

        public int Pid { get; set; }


        // ==========================================
        // PERSONAL INFORMATION
        // ==========================================

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;


        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;


        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        [Display(Name = "Phone")]
        public string? Phone { get; set; }


        // ==========================================
        // USERNAME
        // ==========================================

        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;


        // ==========================================
        // ADDRESS
        // ==========================================

        [Display(Name = "Street Address")]
        public string? StreetAddress { get; set; }


        [Display(Name = "Suburb")]
        public string? Suburb { get; set; }


        [Display(Name = "City")]
        public string? City { get; set; }


        [Display(Name = "Province")]
        public string? Province { get; set; }


        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }


        // ==========================================
        // SKILLS
        // ==========================================

        public List<int> SelectedSkillIds { get; set; } = new List<int>();

        public List<Skill> AvailableSkills { get; set; } = new List<Skill>();

        // ==========================================
        // NEW SKILLS ADDED BY VOLUNTEER
        // ==========================================

        public string? NewSkills { get; set; }
    }
}