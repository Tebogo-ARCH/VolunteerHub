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

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var volunteer = _context.Volunteers
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Vid = volunteer.Vid;

            var organization = _context.Organizations
                .Include(o => o.Address)
                .FirstOrDefault(o => o.Mid == volunteer.Mid);

            ViewBag.Organization = organization;


            return View();
        }

        [HttpGet]
        public IActionResult Profile()
        {

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var person = _context.Persons
                .Include(p => p.Address)
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var volunteer = _context.Volunteers
                .Include(v => v.VolunteerSkills)
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }

            var skills = _context.Skills
                .OrderBy(s => s.SkillName)
                .ToList();


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

            if (person.Address != null)
            {
                model.StreetAddress = person.Address.StreetAddress;

                model.Suburb = person.Address.Suburb;

                model.City = person.Address.City;

                model.Province = person.Address.Province;

                model.PostalCode = person.Address.PostalCode;
            }

            return View(
                "~/Views/Volunteer/Profile/Index.cshtml",
                model
            );
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(VolunteerProfileViewModel model)
        {

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }


            var person = _context.Persons
                .Include(p => p.Address)
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }


            var volunteer = _context.Volunteers
                .Include(v => v.VolunteerSkills)
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }

            model.AvailableSkills = _context.Skills
                .OrderBy(s => s.SkillName)
                .ToList();


            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Volunteer/Profile/Index.cshtml",
                    model
                );
            }


            person.FirstName = model.FirstName;

            person.LastName = model.LastName;

            person.Email = model.Email;

            person.Phone = model.Phone;


            if (person.Address == null)
            {

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

            _context.VolunteerSkills.RemoveRange(
                volunteer.VolunteerSkills
            );


            if (model.SelectedSkillIds != null)
            {
                foreach (var skillId in model.SelectedSkillIds.Distinct())
                {
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

            if (!string.IsNullOrWhiteSpace(model.NewSkills))
            {
                var newSkillNames = model.NewSkills
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();


                foreach (var skillName in newSkillNames)
                {
                    var existingSkill = _context.Skills
                        .FirstOrDefault(s =>
                            s.SkillName.ToLower() == skillName.ToLower());


                    int skillId;


                    if (existingSkill != null)
                    {
                        skillId = existingSkill.SkillId;
                    }
                    else
                    {
                        var newSkill = new Skill
                        {
                            SkillName = skillName
                        };

                        _context.Skills.Add(newSkill);

                        _context.SaveChanges();

                        skillId = newSkill.SkillId;
                    }
                    volunteer.VolunteerSkills.Add(
                        new VolunteerSkill
                        {
                            Vid = volunteer.Vid,
                            SkillId = skillId
                        }
                    );
                }
            }
            _context.SaveChanges();

            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";

            return RedirectToAction("Profile");
        }
    }
}