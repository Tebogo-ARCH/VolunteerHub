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

        // ==========================================
        // VIEW ORGANIZATION
        // ==========================================

        public IActionResult Index()
        {
            // ==========================================
            // GET LOGGED-IN USER
            // ==========================================

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // FIND PERSON
            // ==========================================

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // FIND STAFF
            // ==========================================

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ==========================================
            // FIND ORGANIZATION
            // ==========================================

            var organization = _context.Organizations
                .Include(o => o.Address)
                .Include(o => o.MidNavigation)
                    .ThenInclude(m => m.PidNavigation)
                .FirstOrDefault(o => o.Mid == staff.Mid);

            if (organization == null)
            {
                return NotFound();
            }

            // ==========================================
            // STAFF INFORMATION
            // ==========================================

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Sid = staff.Sid;

            // ==========================================
            // RETURN ORGANIZATION PAGE
            // ==========================================

            return View(
                "~/Views/Staff/Organization/Index.cshtml",
                organization
            );
        }
    }
}