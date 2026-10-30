
using Microsoft.AspNetCore.Mvc;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class ManagerController : Controller
    {
        private readonly VolunteerHubContext _context;

        public ManagerController(VolunteerHubContext context)
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

            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            if (manager == null)
            {
                return Forbid();
            }

            var organization = _context.Organizations
                .FirstOrDefault(o => o.Mid == manager.Mid);

            var staffCount = _context.Staff
                .Count(s => s.Mid == manager.Mid);

            var volunteerCount = _context.Volunteers
                .Count(v => v.Mid == manager.Mid);

            var projectCount = _context.Projects
                .Count(p => p.Mid == manager.Mid);

            var attendanceCount = _context.Attendances
                .Count(a => a.VidNavigation.Mid == manager.Mid);

            var reportCount = _context.Reports
                .Count(r => r.VidNavigation.Mid == manager.Mid);

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            if (organization != null)
            {
                ViewBag.OrganizationName = organization.OrgName;
                ViewBag.OrganizationDescription = organization.Description;
                ViewBag.OrganizationEmail = organization.Email;
                ViewBag.OrganizationPhone = organization.Phone;
                ViewBag.Oid = organization.Oid;
            }
            else
            {
                ViewBag.OrganizationName = null;
            }

            ViewBag.StaffCount = staffCount;
            ViewBag.VolunteerCount = volunteerCount;
            ViewBag.ProjectCount = projectCount;
            ViewBag.AttendanceCount = attendanceCount;
            ViewBag.ReportCount = reportCount;

            return View();
        }
    }
}
