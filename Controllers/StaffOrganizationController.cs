using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class StaffOrganizationController : Controller
    {
        private readonly VolunteerHubContext _context;

        public StaffOrganizationController(VolunteerHubContext context)
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
                return RedirectToAction("Login", "Account");
            }

            var organization = _context.Organizations
                .Include(o => o.Address)
                .Include(o => o.MidNavigation)
                    .ThenInclude(m => m.PidNavigation)
                .FirstOrDefault(o => o.Mid == staff.Mid);

            if (organization == null)
            {
                return NotFound();
            }
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Sid = staff.Sid;

            return View(
                "~/Views/Staff/Organization/Index.cshtml",
                organization
            );
        }
    }
}