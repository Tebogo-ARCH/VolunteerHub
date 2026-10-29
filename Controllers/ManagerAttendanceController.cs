
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    // Handles attendance lists and individual attendance details
    // for the volunteers connected to a manager.
    public class ManagerAttendanceController : Controller
    {
        // Database context used to retrieve attendance,
        // people, volunteer, staff, and project information.
        private readonly VolunteerHubContext _context;

        // Constructor: receives and stores the database context.
        public ManagerAttendanceController(VolunteerHubContext context)
        {
            _context = context;
        }

        // ==========================================
        // ATTENDANCE LIST
        // ==========================================

        // Displays attendance records for this manager's volunteers.
        public IActionResult Index()
        {
            // Retrieve the logged-in username from the session.
            var username = HttpContext.Session.GetString("Username");

            // If the session has no username, redirect to Login.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the person record belonging to the logged-in user.
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            // If the person record cannot be found, return to Login.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the manager record connected to this person.
            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            // If the person is not a manager, return to the
            // manager dashboard action.
            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            // Retrieve attendance records belonging to volunteers
            // whose manager ID matches the logged-in manager.
            //
            // Include() loads related records, while ThenInclude()
            // loads another related record through a navigation property.
            var attendance = _context.Attendances
                .Include(a => a.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Include(a => a.Project)
                .Include(a => a.SidNavigation)
                    .ThenInclude(s => s.PidNavigation)
                .Where(a => a.VidNavigation.Mid == manager.Mid)
                .OrderByDescending(a => a.AttDate)
                .ToList();

            // Pass the manager's personal information to the View.
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            // Return the attendance list to the specified View.
            // The attendance collection is passed as the View's model.
            return View(
                "~/Views/Manager/Attendance/Index.cshtml",
                attendance
            );
        }

        // ==========================================
        // ATTENDANCE DETAILS
        // ==========================================

        // Displays one attendance record selected by its ID.
        public IActionResult Details(int id)
        {
            // Retrieve the logged-in username from the session.
            var username = HttpContext.Session.GetString("Username");

            // If the user is not logged in, redirect to Login.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the person's database record.
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            // If the person cannot be found, return to Login.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the manager record associated with this person.
            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            // If the user does not have a manager record,
            // return them to the manager dashboard.
            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            // Find the selected attendance record.
            //
            // The query checks two conditions:
            // 1. The attendance ID must match the supplied ID.
            // 2. The volunteer must belong to this manager.
            //
            // These conditions prevent the manager from opening
            // an attendance record belonging to another manager.
            var attendance = _context.Attendances
                .Include(a => a.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Include(a => a.Project)
                .Include(a => a.SidNavigation)
                    .ThenInclude(s => s.PidNavigation)
                .FirstOrDefault(a =>
                    a.AttId == id &&
                    a.VidNavigation.Mid == manager.Mid);

            // If no matching attendance record is found,
            // return an HTTP 404 Not Found response.
            if (attendance == null)
            {
                return NotFound();
            }

            // Pass the manager's details to the View.
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            // Return the attendance details View with the
            // selected attendance record as its model.
            return View(
                "~/Views/Manager/Attendance/Details.cshtml",
                attendance
            );
        }
    }
}
