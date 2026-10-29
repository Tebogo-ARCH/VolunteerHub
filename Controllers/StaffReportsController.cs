using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class StaffReportsController : Controller
    {
        private readonly VolunteerHubContext _context;

        public StaffReportsController(VolunteerHubContext context)
        {
            _context = context;
        }

        // ==========================================================
        // REPORTS - INDEX
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
            // GET REPORTS
            // ONLY REPORTS FROM THIS STAFF MEMBER'S ORGANIZATION
            // ======================================================

            var reports = _context.Reports
                .Include(r => r.Project)
                .Include(r => r.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Where(r => r.VidNavigation.Mid == staff.Mid)
                .OrderByDescending(r => r.ReportId)
                .ToList();

            // ======================================================
            // STAFF INFORMATION
            // ======================================================

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;

            // ======================================================
            // RETURN REPORTS VIEW
            // ======================================================

            return View(
                "~/Views/Staff/Reports/Index.cshtml",
                reports
            );
        }
    }
}