using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class StaffController : Controller
    {
        private readonly VolunteerHubContext _context;

        public StaffController(VolunteerHubContext context)
        {
            _context = context;
        }

        // ==========================================
        // STAFF DASHBOARD
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
                .Include(p => p.Staff)
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
            // GET ORGANIZATION
            // ==========================================

            var organization = _context.Organizations
                .Include(o => o.Address)
                .FirstOrDefault(o => o.Mid == staff.Mid);

            // ==========================================
            // GET STAFF PROJECTS
            // ==========================================

            var projectCount = _context.Projects
                .Count(p => p.Mid == staff.Mid);

            // ==========================================
            // GET VOLUNTEER COUNT
            // ==========================================

            var volunteerCount = _context.Volunteers
                .Count(v => v.Mid == staff.Mid);

            // ==========================================
            // GET ATTENDANCE COUNT
            // ==========================================

            var attendanceCount = _context.Attendances
                .Count(a => a.Sid == staff.Sid);

            // ==========================================
            // GET REPORT COUNT
            // ==========================================

            var reportCount = _context.Reports
                .Count(r => r.Mid == staff.Mid);

            // ==========================================
            // VIEW BAG INFORMATION
            // ==========================================

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;

            ViewBag.Sid = staff.Sid;
            ViewBag.Mid = staff.Mid;

            ViewBag.Organization = organization;

            ViewBag.ProjectCount = projectCount;
            ViewBag.VolunteerCount = volunteerCount;
            ViewBag.AttendanceCount = attendanceCount;
            ViewBag.ReportCount = reportCount;

            // ==========================================
            // RETURN DASHBOARD
            // ==========================================

            return View(
                "~/Views/Staff/Index.cshtml"
            );
        }
    }
}