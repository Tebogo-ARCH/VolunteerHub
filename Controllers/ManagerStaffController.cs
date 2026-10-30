
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class ManagerStaffController : Controller
    {
        private readonly VolunteerHubContext _context;

        public ManagerStaffController(VolunteerHubContext context)
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

            var staff = _context.Staff
                .Include(s => s.PidNavigation)
                    .ThenInclude(p => p.Address)
                .Where(s => s.Mid == manager.Mid)
                .ToList();

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Staff/Index.cshtml",
                staff
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

            var staff = _context.Staff
                .Include(s => s.PidNavigation)
                    .ThenInclude(p => p.Address)
                .FirstOrDefault(s =>
                    s.Sid == id &&
                    s.Mid == manager.Mid);

            if (staff == null)
            {
                return NotFound();
            }
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Staff/Details.cshtml",
                staff
            );
        }
    }
}
