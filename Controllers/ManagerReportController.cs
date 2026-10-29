
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    // Handles listing, viewing, and generating volunteer reports.
    public class ManagerReportController : Controller
    {
        // Database context used to access reports, managers,
        // volunteers, projects, assignments, and attendance.
        private readonly VolunteerHubContext _context;

        // Constructor: stores the injected database context.
        public ManagerReportController(VolunteerHubContext context)
        {
            _context = context;
        }

        // =========================================================
        // VIEW REPORTS
        // =========================================================

        // Displays the logged-in manager's reports.
        // The optional search parameter allows the list to be filtered.
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            // CHECK LOGIN

            // Retrieve the username from the session.
            string? username =
                HttpContext.Session.GetString("Username");

            // Redirect to Login if the session has no username.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // FIND PERSON

            // Find the person associated with the username.
            // ToUpper() makes this comparison case-insensitive
            // for ordinary uppercase/lowercase username differences.
            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.UserName.ToUpper() == username.ToUpper());

            // If no person is found, return to Login.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // FIND MANAGER

            // Find the manager record linked to the person.
            var manager = await _context.Managers
                .FirstOrDefaultAsync(m =>
                    m.Pid == person.Pid);

            // Deny access if no manager record exists.
            if (manager == null)
            {
                return Forbid();
            }

            // GET REPORTS BELONGING TO THIS MANAGER

            // Build a query for reports belonging to this manager.
            // Include related volunteer, person, project, and certificate
            // records so the View can display their details.
            //
            // AsQueryable() allows further filters to be added before
            // the query is executed.
            var query = _context.Reports
                .Where(r => r.Mid == manager.Mid)
                .Include(r => r.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Include(r => r.Project)
                .Include(r => r.Certificates)
                .AsQueryable();

            // SEARCH

            // Apply search filters only if the user supplied
            // a search value that is not empty or whitespace.
            if (!string.IsNullOrWhiteSpace(search))
            {
                // Remove spaces from the beginning and end of the search.
                search = search.Trim();

                // Keep reports matching one or more search conditions.
                // The search checks IDs, project details, report status,
                // and the volunteer's first or last name.
                query = query.Where(r =>
                    r.ReportId.ToString().Contains(search) ||
                    r.Vid.ToString().Contains(search) ||
                    r.ProjectId.ToString().Contains(search) ||

                    (r.Project != null &&
                     r.Project.ProjectName.Contains(search)) ||

                    (r.Project != null &&
                     r.Project.Status != null &&
                     r.Project.Status.Contains(search)) ||

                    (r.Status != null &&
                     r.Status.Contains(search)) ||

                    (r.VidNavigation != null &&
                     r.VidNavigation.PidNavigation != null &&
                     (
                         r.VidNavigation.PidNavigation.FirstName.Contains(search) ||
                         r.VidNavigation.PidNavigation.LastName.Contains(search)
                     ))
                );
            }

            // ORDER REPORTS

            // Sort the filtered reports by report ID, newest first.
            // Execute the database query asynchronously and store
            // the results in a list.
            var reports = await query
                .OrderByDescending(r => r.ReportId)
                .ToListAsync();

            // VIEW BAG

            // Pass manager information and search results to the View.
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Username = person.UserName;
            ViewBag.Mid = manager.Mid;
            ViewBag.Search = search;
            ViewBag.ReportCount = reports.Count;

            // RETURN VIEW

            // Display the report list with the reports as its model.
            return View(
                "~/Views/Manager/Reports/Index.cshtml",
                reports
            );
        }


        // =========================================================
        // VIEW REPORT DETAILS
        // =========================================================

        // Displays the details of one report.
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // CHECK LOGIN

            // Retrieve the username from the session.
            string? username =
                HttpContext.Session.GetString("Username");

            // Send the user to Login if no username is stored.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // FIND PERSON

            // Find the person using the session username.
            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.UserName.ToUpper() == username.ToUpper());

            // Redirect to Login if the person cannot be found.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // FIND MANAGER

            // Find the manager record connected to this person.
            var manager = await _context.Managers
                .FirstOrDefaultAsync(m =>
                    m.Pid == person.Pid);

            // Deny access if the person is not a manager.
            if (manager == null)
            {
                return Forbid();
            }

            // GET REPORT

            // Find the selected report and load its related
            // volunteer/person, project, and certificate information.
            //
            // The report ID and manager ID must both match.
            var report = await _context.Reports
                .Include(r => r.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Include(r => r.Project)
                .Include(r => r.Certificates)
                .FirstOrDefaultAsync(r =>
                    r.ReportId == id &&
                    r.Mid == manager.Mid);

            // If the report does not exist or does not belong
            // to this manager, return Not Found.
            if (report == null)
            {
                return NotFound();
            }

            // VIEW BAG

            // Pass the manager's information to the View.
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Username = person.UserName;
            ViewBag.Mid = manager.Mid;

            // RETURN DETAILS VIEW

            // Display the report details page with the selected report.
            return View(
                "~/Views/Manager/Reports/Details.cshtml",
                report
            );
        }


        // =========================================================
        // GENERATE REPORT
        // =========================================================

        // Creates a report for a volunteer's project assignment.
        // The assignment ID identifies which volunteer and project to use.
        [HttpPost]
        // Validates the anti-forgery token submitted by the form.
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int assignId)
        {
            // CHECK LOGIN

            // Retrieve the logged-in username from the session.
            string? username =
                HttpContext.Session.GetString("Username");

            // Redirect to Login if the username is missing.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // FIND PERSON

            // Find the person associated with the session username.
            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.UserName.ToUpper() == username.ToUpper());

            // Redirect to Login if the person cannot be found.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // FIND MANAGER

            // Find the manager record connected to the person.
            var manager = await _context.Managers
                .FirstOrDefaultAsync(m =>
                    m.Pid == person.Pid);

            // Deny access if the user has no manager record.
            if (manager == null)
            {
                return Forbid();
            }

            // FIND ASSIGNMENT

            // Find the selected assignment and load its project.
            //
            // The assignment must:
            // 1. Match the supplied assignment ID.
            // 2. Belong to a project owned by this manager.
            // 3. Not have a DECLINED status.
            var assignment = await _context.ProjectAssignments
                .Include(pa => pa.Project)
                .FirstOrDefaultAsync(pa =>
                    pa.AssignId == assignId &&
                    pa.Project.Mid == manager.Mid &&
                    pa.Status != "DECLINED");

            // If no matching assignment exists, return Not Found.
            if (assignment == null)
            {
                return NotFound();
            }

            // PROJECT MUST BE COMPLETED

            // Reports can only be generated when the project's status
            // is COMPLETED. Other statuses must be rejected.
            if (!string.Equals(
                    assignment.Project.Status,
                    "COMPLETED",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Save an explanation for the user.
                TempData["ErrorMessage"] =
                    "The project must be COMPLETED before generating a report.";

                // Return to the project's completion review page.
                return RedirectToAction(
                    "Completion",
                    "ManagerProjects",
                    new { id = assignment.ProjectId });
            }

            // ASSIGNMENT MUST BE COMPLETED

            // The individual volunteer assignment must also be completed.
            if (!string.Equals(
                    assignment.Status,
                    "COMPLETED",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "The volunteer assignment must be COMPLETED before generating a report.";

                return RedirectToAction(
                    "Completion",
                    "ManagerProjects",
                    new { id = assignment.ProjectId });
            }

            // GET ATTENDANCE

            // Retrieve attendance for this volunteer on this project.
            // Both IDs are checked to avoid mixing records from
            // different volunteers or projects.
            var attendance = await _context.Attendances
                .Where(a =>
                    a.ProjectId == assignment.ProjectId &&
                    a.Vid == assignment.Vid)
                .ToListAsync();

            // ATTENDANCE MUST EXIST

            // A report cannot be generated if there are no
            // attendance records for this volunteer and project.
            if (!attendance.Any())
            {
                TempData["ErrorMessage"] =
                    "A report cannot be generated because no attendance records exist.";

                return RedirectToAction(
                    "Completion",
                    "ManagerProjects",
                    new { id = assignment.ProjectId });
            }

            // ALL ATTENDANCE MUST BE VERIFIED

            // Check whether any attendance record has not been verified.
            // If at least one record is unverified, report generation stops.
            if (attendance.Any(a => a.Verified != true))
            {
                TempData["ErrorMessage"] =
                    "A report cannot be generated until all attendance hours have been verified.";

                return RedirectToAction(
                    "Completion",
                    "ManagerProjects",
                    new { id = assignment.ProjectId });
            }

            // CHECK FOR EXISTING REPORT

            // Search for an existing report for the same volunteer
            // and project to help prevent duplicate reports.
            var existingReport = await _context.Reports
                .FirstOrDefaultAsync(r =>
                    r.Vid == assignment.Vid &&
                    r.ProjectId == assignment.ProjectId);

            // If a report already exists, do not create another.
            if (existingReport != null)
            {
                TempData["ErrorMessage"] =
                    "A report already exists for this volunteer and project.";

                // Redirect to the existing report's details page.
                return RedirectToAction(
                    "Details",
                    new { id = existingReport.ReportId });
            }

            // CALCULATE TOTAL HOURS

            // Add all attendance hours together.
            // Treat a null HoursWorked value as 0.
            decimal totalHours =
                attendance.Sum(a => a.HoursWorked ?? 0);

            // CREATE REPORT

            // Build the report using the assignment's volunteer
            // and project IDs, the calculated hours, and the manager ID.
            var report = new Report
            {
                Vid = assignment.Vid,

                ProjectId = assignment.ProjectId,

                TotalHours = totalHours,

                Mid = manager.Mid,

                // Mark the new report as GENERATED.
                Status = "GENERATED"
            };

            // SAVE REPORT

            // Add the report to the database context.
            _context.Reports.Add(report);

            // Save the report asynchronously.
            await _context.SaveChangesAsync();

            // SUCCESS MESSAGE

            // Store a message that can be displayed after redirecting.
            TempData["SuccessMessage"] =
                "Volunteer report generated successfully.";

            // GO TO REPORT DETAILS

            // After saving, redirect to the newly created report.
            // SaveChangesAsync populates the generated ReportId.
            return RedirectToAction(
                "Details",
                new { id = report.ReportId });
        }
    }
}
