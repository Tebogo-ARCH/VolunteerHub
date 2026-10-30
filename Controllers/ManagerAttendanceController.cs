
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class ManagerAttendanceController : Controller
    {
        private readonly VolunteerHubContext _context;

        public ManagerAttendanceController(VolunteerHubContext context)
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
                return RedirectToAction("Index", "Manager");
            }

            var attendance = _context.Attendances
                .Include(a => a.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Include(a => a.Project)
                .Include(a => a.SidNavigation)
                    .ThenInclude(s => s.PidNavigation)
                .Where(a => a.VidNavigation.Mid == manager.Mid)
                .OrderByDescending(a => a.AttDate)
                .ToList();

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Attendance/Index.cshtml",
                attendance
            );
        }

        public IActionResult Details(int id)
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
                return RedirectToAction("Index", "Manager");
            }

            var attendance = _context.Attendances
                .Include(a => a.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Include(a => a.Project)
                .Include(a => a.SidNavigation)
                    .ThenInclude(s => s.PidNavigation)
                .FirstOrDefault(a =>
                    a.AttId == id &&
                    a.VidNavigation.Mid == manager.Mid);

            if (attendance == null)
            {
                return NotFound();
            }

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Attendance/Details.cshtml",
                attendance
            );
        }
    }
}
