
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    // Handles the manager's staff list and staff details pages.
    public class ManagerStaffController : Controller
    {
        // Provides access to the VolunteerHub database.
        private readonly VolunteerHubContext _context;

        // Constructor: receives and stores the database context.
        public ManagerStaffController(VolunteerHubContext context)
        {
            _context = context;
        }

        // ==========================================
        // STAFF LIST
        // ==========================================

        // Displays all staff members connected to this manager.
        public IActionResult Index()
        {
            // Retrieve the logged-in username from the session.
            var username = HttpContext.Session.GetString("Username");

            // If the username is missing, redirect to Login.
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

            // Find the manager record connected to this person.
            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            // If the person has no manager record, return to
            // the manager dashboard.
            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            // Retrieve staff members who belong to this manager.
            //
            // Include() loads each staff member's related Person record.
            // ThenInclude() also loads that person's address.
            // Where() filters the results using the manager's ID.
            var staff = _context.Staff
                .Include(s => s.PidNavigation)
                    .ThenInclude(p => p.Address)
                .Where(s => s.Mid == manager.Mid)
                .ToList();

            // Pass manager information to the staff list View.
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            // Display the staff list and pass the staff collection
            // as the View's model.
            return View(
                "~/Views/Manager/Staff/Index.cshtml",
                staff
            );
        }


        // ==========================================
        // STAFF DETAILS
        // ==========================================

        // Displays details for one staff member selected by ID.
        public IActionResult Details(int id)
        {
            // Retrieve the username from the session.
            var username = HttpContext.Session.GetString("Username");

            // Redirect to Login if the user is not logged in.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the person associated with the logged-in username.
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            // If the person does not exist, return to Login.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the manager record linked to the person.
            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            // Return to the manager dashboard if the manager record
            // cannot be found.
            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            // Find the selected staff member.
            //
            // The query checks both the staff ID and the manager ID.
            // This ensures that the selected staff member belongs
            // to the logged-in manager.
            //
            // The related Person and Address records are loaded
            // so their details can be displayed in the View.
            var staff = _context.Staff
                .Include(s => s.PidNavigation)
                    .ThenInclude(p => p.Address)
                .FirstOrDefault(s =>
                    s.Sid == id &&
                    s.Mid == manager.Mid);

            // If the staff member cannot be found, or does not belong
            // to this manager, return a 404 Not Found response.
            if (staff == null)
            {
                return NotFound();
            }

            // Pass manager information to the details View.
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            // Display the selected staff member's details.
            return View(
                "~/Views/Manager/Staff/Details.cshtml",
                staff
            );
        }
    }
}
