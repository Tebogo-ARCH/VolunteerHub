using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class VolunteerProfileController : Controller
    {
        private readonly VolunteerHubContext _context;

        public VolunteerProfileController(VolunteerHubContext context)
        {
            _context = context;
        }

        // ==========================================================
        // GET PROFILE
        // ==========================================================

        [HttpGet]
        public IActionResult Index()
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
                .Include(v => v.PidNavigation)
                    .ThenInclude(p => p.Address)
                .Include(v => v.VolunteerSkills)
                    .ThenInclude(vs => vs.Skill)
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return NotFound();
            }

            return View(
                "~/Views/Volunteer/Profile/Index.cshtml",
                volunteer
            );
        }


        // ==========================================================
        // EDIT PROFILE - GET
        // ==========================================================

        [HttpGet]
        public IActionResult Edit()
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
                .Include(v => v.PidNavigation)
                    .ThenInclude(p => p.Address)
                .Include(v => v.VolunteerSkills)
                    .ThenInclude(vs => vs.Skill)
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return NotFound();
            }

            ViewBag.AvailableSkills = _context.Skills
                .OrderBy(s => s.SkillName)
                .ToList();

            return View(
                "~/Views/Volunteer/Profile/Edit.cshtml",
                volunteer
            );
        }


        // ==========================================================
        // EDIT PROFILE - POST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            int vid,
            string firstName,
            string lastName,
            string email,
            string phone,
            string streetAddress,
            string suburb,
            string city,
            string province,
            string postalCode,
            List<int>? selectedSkillIds)
        {
            // ======================================================
            // CHECK LOGIN
            // ======================================================

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }


            // ======================================================
            // FIND LOGGED-IN PERSON
            // ======================================================

            var person = _context.Persons
                .Include(p => p.Address)
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }


            // ======================================================
            // FIND LOGGED-IN VOLUNTEER
            // ======================================================

            var volunteer = _context.Volunteers
                .Include(v => v.PidNavigation)
                    .ThenInclude(p => p.Address)
                .Include(v => v.VolunteerSkills)
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return NotFound();
            }


            // ======================================================
            // UPDATE PERSONAL INFORMATION
            // ======================================================

            volunteer.PidNavigation.FirstName = firstName;
            volunteer.PidNavigation.LastName = lastName;
            volunteer.PidNavigation.Email = email;
            volunteer.PidNavigation.Phone = phone;


            // ======================================================
            // UPDATE ADDRESS
            // ======================================================

            if (volunteer.PidNavigation.Address == null)
            {
                volunteer.PidNavigation.Address = new Address();
            }

            volunteer.PidNavigation.Address.StreetAddress = streetAddress;
            volunteer.PidNavigation.Address.Suburb = suburb;
            volunteer.PidNavigation.Address.City = city;
            volunteer.PidNavigation.Address.Province = province;
            volunteer.PidNavigation.Address.PostalCode = postalCode;


            // ======================================================
            // REMOVE OLD SKILLS
            // ======================================================

            var oldSkills = _context.VolunteerSkills
                .Where(vs => vs.Vid == volunteer.Vid)
                .ToList();

            _context.VolunteerSkills.RemoveRange(oldSkills);


            // ======================================================
            // ADD NEW SKILLS
            // ======================================================

            selectedSkillIds ??= new List<int>();

            foreach (var skillId in selectedSkillIds.Distinct())
            {
                var skillExists = _context.Skills
                    .Any(s => s.SkillId == skillId);

                if (!skillExists)
                {
                    continue;
                }

                var volunteerSkill = new VolunteerSkill
                {
                    Vid = volunteer.Vid,
                    SkillId = skillId
                };

                _context.VolunteerSkills.Add(volunteerSkill);
            }


            // ======================================================
            // SAVE
            // ======================================================

            _context.SaveChanges();


            // ======================================================
            // RETURN TO PROFILE
            // ======================================================

            return RedirectToAction("Index");
        }
    }
}