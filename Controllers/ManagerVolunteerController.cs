
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    // Handles the manager's volunteer list and volunteer details pages.
    public class ManagerVolunteerController : Controller
    {
        // Provides access to volunteer and related database records.
        private readonly VolunteerHubContext _context;

        // Constructor: receives and stores the database context.
        public ManagerVolunteerController(VolunteerHubContext context)
        {
            _context = context;
        }

        // ==========================================
        // VOLUNTEER LIST
        // ==========================================

        // Displays all volunteers connected to this manager.
        public IActionResult Index()
        {
            // Retrieve the logged-in username from the session.
            var username = HttpContext.Session.GetString("Username");

            // If the user is not logged in, redirect to Login.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the person associated with the username.
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            // If the person cannot be found, return to Login.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the manager record linked to this person.
            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            // If the person has no manager record, return to
            // the manager dashboard.
            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            // Retrieve volunteers belonging to this manager.
            //
            // Include() loads each volunteer's related Person record.
            // ThenInclude() loads the person's address.
            // Where() limits the results to this manager's volunteers.
            var volunteers = _context.Volunteers
                .Include(v => v.PidNavigation)
                    .ThenInclude(p => p.Address)
                .Where(v => v.Mid == manager.Mid)
                .ToList();

            // Pass the manager's information to the volunteer list View.
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            // Return the volunteer list and pass the collection
            // as the View's model.
            return View(
                "~/Views/Manager/Volunteers/Index.cshtml",
                volunteers
            );
        }


        // ==========================================
        // VOLUNTEER DETAILS
        // ==========================================

        // Displays the details of one selected volunteer.
        public IActionResult Details(int id)
        {
            // Retrieve the username saved during login.
            var username = HttpContext.Session.GetString("Username");

            // Redirect to Login if the session has no username.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the person record using the username.
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            // If the person cannot be found, return to Login.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the manager record associated with the person.
            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            // If no manager record exists, return to the dashboard.
            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            // Find the volunteer selected by the supplied ID.
            //
            // Load the volunteer's related Person and Address records.
            // Check both the volunteer ID and manager ID to ensure
            // the volunteer belongs to this manager.
            var volunteer = _context.Volunteers
                .Include(v => v.PidNavigation)
                    .ThenInclude(p => p.Address)
                .FirstOrDefault(v =>
                    v.Vid == id &&
                    v.Mid == manager.Mid);

            // If no matching volunteer is found, return Not Found.
            if (volunteer == null)
            {
                return NotFound();
            }

            // Pass the manager's information to the details View.
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            // Display the selected volunteer's details.
            return View(
                "~/Views/Manager/Volunteers/Details.cshtml",
                volunteer
            );
        }
    }
}
