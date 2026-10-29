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


        // ==========================================================
        // VOLUNTEER LIST
        // ==========================================================

        public IActionResult Index()
        {
            // ======================================================
            // GET LOGGED-IN USER
            // ======================================================

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }


            // ======================================================
            // FIND PERSON
            // ======================================================

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }


            // ======================================================
            // FIND STAFF
            // ======================================================

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }


            // ======================================================
            // GET VOLUNTEERS FROM STAFF'S ORGANIZATION
            // INCLUDING THEIR SKILLS
            // ======================================================

            var volunteers = _context.Volunteers

                .Include(v => v.PidNavigation)

                .Include(v => v.VolunteerSkills)
                    .ThenInclude(vs => vs.Skill)

                .Where(v => v.Mid == staff.Mid)

                .OrderBy(v => v.PidNavigation.FirstName)
                .ThenBy(v => v.PidNavigation.LastName)

                .ToList();


            // ======================================================
            // STAFF INFORMATION
            // ======================================================

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;
            ViewBag.UserName = person.UserName;


            // ======================================================
            // RETURN VIEW
            // ======================================================

            return View(
                "~/Views/Staff/Volunteers/Index.cshtml",
                volunteers
            );
        }
    }
}