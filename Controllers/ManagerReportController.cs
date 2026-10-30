
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class ManagerReportController : Controller
    {
        private readonly VolunteerHubContext _context;

        public ManagerReportController(VolunteerHubContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            string? username =
                HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.UserName.ToUpper() == username.ToUpper());

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var manager = await _context.Managers
                .FirstOrDefaultAsync(m =>
                    m.Pid == person.Pid);

            if (manager == null)
            {
                return Forbid();
            }

            var query = _context.Reports
                .Where(r => r.Mid == manager.Mid)
                .Include(r => r.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Include(r => r.Project)
                .Include(r => r.Certificates)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

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

            var reports = await query
                .OrderByDescending(r => r.ReportId)
                .ToListAsync();

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Username = person.UserName;
            ViewBag.Mid = manager.Mid;
            ViewBag.Search = search;
            ViewBag.ReportCount = reports.Count;

            return View(
                "~/Views/Manager/Reports/Index.cshtml",
                reports
            );
        }


        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            string? username =
                HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.UserName.ToUpper() == username.ToUpper());

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var manager = await _context.Managers
                .FirstOrDefaultAsync(m =>
                    m.Pid == person.Pid);

            if (manager == null)
            {
                return Forbid();
            }

            var report = await _context.Reports
                .Include(r => r.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Include(r => r.Project)
                .Include(r => r.Certificates)
                .FirstOrDefaultAsync(r =>
                    r.ReportId == id &&
                    r.Mid == manager.Mid);

            if (report == null)
            {
                return NotFound();
            }

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Username = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Reports/Details.cshtml",
                report
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int assignId)
        {
            string? username =
                HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.UserName.ToUpper() == username.ToUpper());

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var manager = await _context.Managers
                .FirstOrDefaultAsync(m =>
                    m.Pid == person.Pid);

            if (manager == null)
            {
                return Forbid();
            }

            var assignment = await _context.ProjectAssignments
                .Include(pa => pa.Project)
                .FirstOrDefaultAsync(pa =>
                    pa.AssignId == assignId &&
                    pa.Project.Mid == manager.Mid &&
                    pa.Status != "DECLINED");
            if (assignment == null)
            {
                return NotFound();
            }

            if (!string.Equals(
                    assignment.Project.Status,
                    "COMPLETED",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "The project must be COMPLETED before generating a report.";

                return RedirectToAction(
                    "Completion",
                    "ManagerProjects",
                    new { id = assignment.ProjectId });
            }

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

            var attendance = await _context.Attendances
                .Where(a =>
                    a.ProjectId == assignment.ProjectId &&
                    a.Vid == assignment.Vid)
                .ToListAsync();

            if (!attendance.Any())
            {
                TempData["ErrorMessage"] =
                    "A report cannot be generated because no attendance records exist.";

                return RedirectToAction(
                    "Completion",
                    "ManagerProjects",
                    new { id = assignment.ProjectId });
            }

            if (attendance.Any(a => a.Verified != true))
            {
                TempData["ErrorMessage"] =
                    "A report cannot be generated until all attendance hours have been verified.";

                return RedirectToAction(
                    "Completion",
                    "ManagerProjects",
                    new { id = assignment.ProjectId });
            }

            var existingReport = await _context.Reports
                .FirstOrDefaultAsync(r =>
                    r.Vid == assignment.Vid &&
                    r.ProjectId == assignment.ProjectId);

            if (existingReport != null)
            {
                TempData["ErrorMessage"] =
                    "A report already exists for this volunteer and project.";

                return RedirectToAction(
                    "Details",
                    new { id = existingReport.ReportId });
            }

            decimal totalHours =
                attendance.Sum(a => a.HoursWorked ?? 0);

            var report = new Report
            {
                Vid = assignment.Vid,

                ProjectId = assignment.ProjectId,

                TotalHours = totalHours,

                Mid = manager.Mid,

                Status = "GENERATED"
            };

            _context.Reports.Add(report);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Volunteer report generated successfully.";

            return RedirectToAction(
                "Details",
                new { id = report.ReportId });
        }
    }
}
