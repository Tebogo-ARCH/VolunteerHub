
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class ManagerVolunteerController : Controller
    {
        private readonly VolunteerHubContext _context;

        public ManagerVolunteerController(VolunteerHubContext context)
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

            var volunteers = _context.Volunteers
                .Include(v => v.PidNavigation)
                    .ThenInclude(p => p.Address)
                .Where(v => v.Mid == manager.Mid)
                .ToList();

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Volunteers/Index.cshtml",
                volunteers
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

            var volunteer = _context.Volunteers
                .Include(v => v.PidNavigation)
                    .ThenInclude(p => p.Address)
                .FirstOrDefault(v =>
                    v.Vid == id &&
                    v.Mid == manager.Mid);

            if (volunteer == null)
            {
                return NotFound();
            }

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Volunteers/Details.cshtml",
                volunteer
            );
        }
    }
}
