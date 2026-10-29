using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;
using VolunteerHub.Models.ViewModels;

namespace VolunteerHub.Controllers
{
    public class VolunteerController : Controller
    {
        private readonly VolunteerHubContext _context;

        public VolunteerController(VolunteerHubContext context)
        {
            _context = context;
        }


        // ==========================================
        // VOLUNTEER DASHBOARD
        // ==========================================
        public IActionResult Index()
        {
            // Get logged-in username from session
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }


            // ==========================================
            // GET PERSON
            // ==========================================
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }


            // ==========================================
            // GET VOLUNTEER
            // ==========================================
            var volunteer = _context.Volunteers
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }


            // ==========================================
            // VOLUNTEER INFORMATION
            // ==========================================
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Vid = volunteer.Vid;


            // ==========================================
            // GET ORGANIZATION
            // ==========================================
            var organization = _context.Organizations
                .Include(o => o.Address)
                .FirstOrDefault(o => o.Mid == volunteer.Mid);

            ViewBag.Organization = organization;


            return View();
        }


        // ==========================================
        // VIEW VOLUNTEER PROFILE
        // ==========================================
        [HttpGet]
        public IActionResult Profile()
        {
            // ==========================================
            // GET LOGGED-IN USERNAME
            // ==========================================

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }


            // ==========================================
            // GET PERSON
            // ==========================================

            var person = _context.Persons
                .Include(p => p.Address)
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }


            // ==========================================
            // GET VOLUNTEER
            // ==========================================

            var volunteer = _context.Volunteers
                .Include(v => v.VolunteerSkills)
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }


            // ==========================================
            // GET ALL AVAILABLE SKILLS
            // ==========================================

            var skills = _context.Skills
                .OrderBy(s => s.SkillName)
                .ToList();


            // ==========================================
            // CREATE PROFILE VIEW MODEL
            // ==========================================

            var model = new VolunteerProfileViewModel
            {
                Vid = volunteer.Vid,

                Pid = person.Pid,

                FirstName = person.FirstName,

                LastName = person.LastName,

                Email = person.Email,

                Phone = person.Phone,

                UserName = person.UserName,

                AvailableSkills = skills,

                SelectedSkillIds = volunteer.VolunteerSkills
                    .Select(vs => vs.SkillId)
                    .ToList()
            };


            // ==========================================
            // LOAD ADDRESS
            // ==========================================

            if (person.Address != null)
            {
                model.StreetAddress = person.Address.StreetAddress;

                model.Suburb = person.Address.Suburb;

                model.City = person.Address.City;

                model.Province = person.Address.Province;

                model.PostalCode = person.Address.PostalCode;
            }


            // ==========================================
            // OPEN PROFILE PAGE
            // ==========================================

            return View(
                "~/Views/Volunteer/Profile/Index.cshtml",
                model
            );
        }


        // ==========================================
        // UPDATE VOLUNTEER PROFILE
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(VolunteerProfileViewModel model)
        {
            // ==========================================
            // GET LOGGED-IN USERNAME
            // ==========================================

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }


            // ==========================================
            // GET PERSON
            // ==========================================

            var person = _context.Persons
                .Include(p => p.Address)
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }


            // ==========================================
            // GET VOLUNTEER
            // ==========================================

            var volunteer = _context.Volunteers
                .Include(v => v.VolunteerSkills)
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }


            // ==========================================
            // GET AVAILABLE SKILLS
            // ==========================================

            model.AvailableSkills = _context.Skills
                .OrderBy(s => s.SkillName)
                .ToList();


            // ==========================================
            // VALIDATE FORM
            // ==========================================

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Volunteer/Profile/Index.cshtml",
                    model
                );
            }


            // ==========================================
            // UPDATE PERSONAL INFORMATION
            // ==========================================

            person.FirstName = model.FirstName;

            person.LastName = model.LastName;

            person.Email = model.Email;

            person.Phone = model.Phone;


            // ==========================================
            // UPDATE ADDRESS
            // ==========================================

            if (person.Address == null)
            {
                // ======================================
                // CREATE NEW ADDRESS
                // ======================================

                var newAddress = new Address
                {
                    StreetAddress = model.StreetAddress ?? "",

                    Suburb = model.Suburb,

                    City = model.City ?? "",

                    Province = model.Province ?? "",

                    PostalCode = model.PostalCode
                };

                _context.Addresses.Add(newAddress);

                _context.SaveChanges();

                person.AddressId = newAddress.AddressId;

                person.Address = newAddress;
            }
            else
            {
                // ======================================
                // UPDATE EXISTING ADDRESS
                // ======================================

                person.Address.StreetAddress =
                    model.StreetAddress ?? "";

                person.Address.Suburb =
                    model.Suburb;

                person.Address.City =
                    model.City ?? "";

                person.Address.Province =
                    model.Province ?? "";

                person.Address.PostalCode =
                    model.PostalCode;
            }


            // ==========================================
            // UPDATE VOLUNTEER SKILLS
            // ==========================================

            // Remove the volunteer's existing skills
            _context.VolunteerSkills.RemoveRange(
                volunteer.VolunteerSkills
            );


            // ==========================================
            // ADD EXISTING SELECTED SKILLS
            // ==========================================

            if (model.SelectedSkillIds != null)
            {
                foreach (var skillId in model.SelectedSkillIds.Distinct())
                {
                    // Check that the skill exists
                    var skillExists = _context.Skills
                        .Any(s => s.SkillId == skillId);

                    if (skillExists)
                    {
                        volunteer.VolunteerSkills.Add(
                            new VolunteerSkill
                            {
                                Vid = volunteer.Vid,
                                SkillId = skillId
                            }
                        );
                    }
                }
            }


            // ==========================================
            // ADD NEW SKILLS
            // ==========================================

            if (!string.IsNullOrWhiteSpace(model.NewSkills))
            {
                // Split skills using commas
                var newSkillNames = model.NewSkills
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();


                foreach (var skillName in newSkillNames)
                {
                    // Check if this skill already exists
                    var existingSkill = _context.Skills
                        .FirstOrDefault(s =>
                            s.SkillName.ToLower() == skillName.ToLower());


                    int skillId;


                    if (existingSkill != null)
                    {
                        // ======================================
                        // SKILL ALREADY EXISTS
                        // ======================================

                        skillId = existingSkill.SkillId;
                    }
                    else
                    {
                        // ======================================
                        // CREATE NEW SKILL
                        // ======================================

                        var newSkill = new Skill
                        {
                            SkillName = skillName
                        };

                        _context.Skills.Add(newSkill);

                        // Save so the new Skill gets its SkillId
                        _context.SaveChanges();

                        skillId = newSkill.SkillId;
                    }


                    // ======================================
                    // LINK SKILL TO VOLUNTEER
                    // ======================================

                    volunteer.VolunteerSkills.Add(
                        new VolunteerSkill
                        {
                            Vid = volunteer.Vid,
                            SkillId = skillId
                        }
                    );
                }
            }


            // ==========================================
            // SAVE ALL CHANGES
            // ==========================================

            _context.SaveChanges();


            // ==========================================
            // SUCCESS MESSAGE
            // ==========================================

            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";


            // ==========================================
            // RETURN TO PROFILE
            // ==========================================

            return RedirectToAction("Profile");
        }
    }
}