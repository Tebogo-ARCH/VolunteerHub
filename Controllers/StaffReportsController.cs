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


            var reports = _context.Reports
                .Include(r => r.Project)
                .Include(r => r.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Where(r => r.VidNavigation.Mid == staff.Mid)
                .OrderByDescending(r => r.ReportId)
                .ToList();


            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;


            return View(
                "~/Views/Staff/Reports/Index.cshtml",
                reports
            );
        }
    }
}