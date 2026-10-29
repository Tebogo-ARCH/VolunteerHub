
using Microsoft.AspNetCore.Mvc;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    // This controller handles the manager's main dashboard.
    public class ManagerController : Controller
    {
        // Stores the database context so this controller can
        // retrieve information from the VolunteerHub database.
        private readonly VolunteerHubContext _context;

        // Constructor: receives the database context through
        // dependency injection and stores it in _context.
        public ManagerController(VolunteerHubContext context)
        {
            _context = context;
        }

        // Displays the manager's dashboard.
        public IActionResult Index()
        {
            // Retrieve the username saved in the session
            // when the user successfully logged in.
            var username = HttpContext.Session.GetString("Username");

            // Check whether the user is logged in.
            // If the username is missing or empty, send the
            // user back to the Account Login page.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the person's database record using the
            // username stored in the session.
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            // If no matching person is found, return to Login.
            // This prevents the dashboard from loading without
            // a valid person record.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the manager record connected to this person.
            // Pid links the Person record to the Manager record.
            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            // If this person does not have a manager record,
            // deny access to the manager dashboard.
            if (manager == null)
            {
                return Forbid();
            }

            // Find the organization managed by this manager.
            // The manager's Mid identifies the organization.
            var organization = _context.Organizations
                .FirstOrDefault(o => o.Mid == manager.Mid);

            // Count all staff members connected to this manager.
            // Count() returns the number of matching staff records.
            var staffCount = _context.Staff
                .Count(s => s.Mid == manager.Mid);

            // Count all volunteers connected to this manager.
            var volunteerCount = _context.Volunteers
                .Count(v => v.Mid == manager.Mid);

            // Count all projects belonging to this manager.
            var projectCount = _context.Projects
                .Count(p => p.Mid == manager.Mid);

            // Count attendance records belonging to volunteers
            // connected to this manager.
            var attendanceCount = _context.Attendances
                .Count(a => a.VidNavigation.Mid == manager.Mid);

            // Count reports belonging to this manager's volunteers.
            var reportCount = _context.Reports
                .Count(r => r.VidNavigation.Mid == manager.Mid);

            // Send the manager's personal information to the View.
            // ViewBag allows the View to access these values
            // without creating a separate ViewModel.
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            // Check whether an organization was found.
            // If it exists, send its details to the dashboard.
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
                // If no organization was found, set its name to null.
                // The View can use this to determine what to display.
                ViewBag.OrganizationName = null;
            }

            // Send the totals to the dashboard.
            // These values can be displayed in dashboard summary cards.
            ViewBag.StaffCount = staffCount;
            ViewBag.VolunteerCount = volunteerCount;
            ViewBag.ProjectCount = projectCount;
            ViewBag.AttendanceCount = attendanceCount;
            ViewBag.ReportCount = reportCount;

            // Return the dashboard View.
            // The View can now display the manager's details,
            // organization information, and summary totals.
            return View();
        }
    }
}
