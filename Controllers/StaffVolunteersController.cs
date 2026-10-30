using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class StaffVolunteersController : Controller
    {
        private readonly VolunteerHubContext _context;

        public StaffVolunteersController(VolunteerHubContext context)
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

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            var volunteers = _context.Volunteers

                .Include(v => v.PidNavigation)

                .Include(v => v.VolunteerSkills)
                    .ThenInclude(vs => vs.Skill)

                .Where(v => v.Mid == staff.Mid)

                .OrderBy(v => v.PidNavigation.FirstName)
                .ThenBy(v => v.PidNavigation.LastName)

                .ToList();

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;
            ViewBag.UserName = person.UserName;

            return View(
                "~/Views/Staff/Volunteers/Index.cshtml",
                volunteers
            );
        }
    }
}