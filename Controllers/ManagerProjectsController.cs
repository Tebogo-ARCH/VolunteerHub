using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class ManagerProjectsController : Controller
    {
        private readonly VolunteerHubContext _context;

        public ManagerProjectsController(VolunteerHubContext context)
        {
            _context = context;
        }

        [HttpGet]
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

            var projects = _context.Projects
                .Where(p => p.Mid == manager.Mid)
                .OrderByDescending(p => p.StartDate)
                .ToList();

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Projects/Index.cshtml",
                projects
            );
        }


        [HttpGet]
        public async Task<IActionResult> Details(int id)
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

            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.Mid == manager.Mid);

            if (project == null)
            {
                return NotFound();
            }

            ViewBag.RequestedVolunteersCount =
                await _context.ProjectAssignments.CountAsync(pa =>
                    pa.ProjectId == id &&
                    pa.Status != null &&
                    pa.Status.ToUpper() == "PENDING");

            ViewBag.AcceptedVolunteersCount =
                await _context.ProjectAssignments.CountAsync(pa =>
                    pa.ProjectId == id &&
                    pa.Status != null &&
                    pa.Status.ToUpper() == "ACCEPTED");

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Projects/Details.cshtml",
                project
            );
        }


        [HttpGet]
        public IActionResult Create()
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

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Projects/Create.cshtml"
            );
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            string projectName,
            string? description,
            DateOnly startDate,
            DateOnly? endDate,
            string? location)
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

            if (string.IsNullOrWhiteSpace(projectName))
            {
                ModelState.AddModelError(
                    "projectName",
                    "Project name is required."
                );
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                ModelState.AddModelError(
                    "location",
                    "Project location is required."
                );
            }

            if (endDate.HasValue && endDate.Value < startDate)
            {
                ModelState.AddModelError(
                    "endDate",
                    "End date cannot be before the start date."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.FirstName = person.FirstName;
                ViewBag.LastName = person.LastName;
                ViewBag.UserName = person.UserName;
                ViewBag.Mid = manager.Mid;

                return View(
                    "~/Views/Manager/Projects/Create.cshtml"
                );
            }

            var project = new Project
            {
                ProjectName = projectName,
                Description = description,
                StartDate = startDate,
                EndDate = endDate,
                Location = location,
                Mid = manager.Mid,
                Status = "PLANNING"
            };

            _context.Projects.Add(project);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Activate(int id)
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
                return Forbid();
            }

            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.Mid == manager.Mid);

            if (project == null)
            {
                return NotFound();
            }

            if (!string.Equals(
                    project.Status,
                    "PLANNING",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only projects with PLANNING status can be activated.";

                return RedirectToAction(nameof(Details), new { id });
            }

            project.Status = "ACTIVE";
            _context.SaveChanges();

            TempData["Success"] =
                "The project has been activated successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cancel(int id)
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
                return Forbid();
            }

            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.Mid == manager.Mid);

            if (project == null)
            {
                return NotFound();
            }

            var currentStatus = project.Status?.ToUpperInvariant();

            if (currentStatus != "PLANNING" &&
                currentStatus != "ACTIVE")
            {
                TempData["Error"] =
                    "Only projects with PLANNING or ACTIVE status can be cancelled.";

                return RedirectToAction(nameof(Details), new { id });
            }

            project.Status = "CANCELLED";
            _context.SaveChanges();

            TempData["Success"] =
                "The project has been cancelled successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }


        [HttpGet]
        public IActionResult Completion(int id)
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
                return Forbid();
            }

            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.Mid == manager.Mid);

            if (project == null)
            {
                return NotFound();
            }

            var assignments = _context.ProjectAssignments
                .Include(pa => pa.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Include(pa => pa.SidNavigation)
                    .ThenInclude(s => s.PidNavigation)
                .Where(pa =>
                    pa.ProjectId == id &&
                    pa.Status != "DECLINED")
                .OrderBy(pa => pa.VidNavigation.PidNavigation.FirstName)
                .ThenBy(pa => pa.VidNavigation.PidNavigation.LastName)
                .ToList();

            var attendance = _context.Attendances
                .Where(a => a.ProjectId == id)
                .ToList();

            var rows = new List<ManagerCompletionRow>();

            foreach (var assignment in assignments)
            {
                var volunteerAttendance = attendance
                    .Where(a => a.Vid == assignment.Vid)
                    .ToList();

                var volunteerName =
                    assignment.VidNavigation.PidNavigation.FirstName
                    + " "
                    + assignment.VidNavigation.PidNavigation.LastName;

                var staffName =
                    assignment.SidNavigation.PidNavigation.FirstName
                    + " "
                    + assignment.SidNavigation.PidNavigation.LastName;

                var attendanceCount = volunteerAttendance.Count;

                var verifiedAttendanceCount =
                    volunteerAttendance.Count(a => a.Verified == true);

                var totalHours =
                    volunteerAttendance.Sum(a => a.HoursWorked ?? 0);

                var hasAttendance = volunteerAttendance.Any();

                var allAttendanceVerified =
                    hasAttendance &&
                    volunteerAttendance.All(a => a.Verified == true);

                var assignmentCompleted =
                    string.Equals(
                        assignment.Status,
                        "COMPLETED",
                        StringComparison.OrdinalIgnoreCase
                    );

                var volunteerCompleted =
                    assignmentCompleted && allAttendanceVerified;

                rows.Add(
                    new ManagerCompletionRow
                    {
                        AssignId = assignment.AssignId,
                        Vid = assignment.Vid,
                        VolunteerName = volunteerName,
                        StaffName = staffName,
                        AssignmentStatus = assignment.Status,
                        AttendanceCount = attendanceCount,
                        VerifiedAttendanceCount = verifiedAttendanceCount,
                        TotalHours = totalHours,
                        HasAttendance = hasAttendance,
                        AllAttendanceVerified = allAttendanceVerified,
                        AssignmentCompleted = assignmentCompleted,
                        VolunteerCompleted = volunteerCompleted
                    }
                );
            }

            bool projectReady = false;

            if (assignments.Any())
            {
                projectReady = rows.All(r => r.VolunteerCompleted);
            }

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            ViewBag.Project = project;
            ViewBag.CompletionRows = rows;
            ViewBag.ProjectReady = projectReady;

            return View(
                "~/Views/Manager/Projects/Completion.cshtml",
                project
            );
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteProject(int id)
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
                return Forbid();
            }

            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.Mid == manager.Mid);

            if (project == null)
            {
                return NotFound();
            }

            var assignments = _context.ProjectAssignments
                .Where(pa =>
                    pa.ProjectId == id &&
                    pa.Status != "DECLINED")
                .ToList();

            if (!assignments.Any())
            {
                TempData["ErrorMessage"] =
                    "This project cannot be completed because there are no active volunteer assignments.";

                return RedirectToAction(
                    nameof(Completion),
                    new { id = id }
                );
            }

            var attendance = _context.Attendances
                .Where(a => a.ProjectId == id)
                .ToList();

            foreach (var assignment in assignments)
            {
                var assignmentCompleted =
                    string.Equals(
                        assignment.Status,
                        "COMPLETED",
                        StringComparison.OrdinalIgnoreCase
                    );

                if (!assignmentCompleted)
                {
                    TempData["ErrorMessage"] =
                        "The project cannot be completed because one or more volunteer assignments are not yet marked as COMPLETED.";

                    return RedirectToAction(
                        nameof(Completion),
                        new { id = id }
                    );
                }

                var volunteerAttendance = attendance
                    .Where(a => a.Vid == assignment.Vid)
                    .ToList();

                if (!volunteerAttendance.Any())
                {
                    TempData["ErrorMessage"] =
                        "The project cannot be completed because one or more volunteers have no attendance records.";

                    return RedirectToAction(
                        nameof(Completion),
                        new { id = id }
                    );
                }

                var allVerified =
                    volunteerAttendance.All(a => a.Verified == true);

                if (!allVerified)
                {
                    TempData["ErrorMessage"] =
                        "The project cannot be completed because one or more attendance records have not been verified by the volunteer.";

                    return RedirectToAction(
                        nameof(Completion),
                        new { id = id }
                    );
                }
            }

            project.Status = "COMPLETED";
            _context.SaveChanges();

            TempData["SuccessMessage"] =
                "The project has been marked as COMPLETED successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = project.ProjectId }
            );
        }
    }


    public class ManagerCompletionRow
    {
        public int AssignId { get; set; }

        public int Vid { get; set; }

        public string VolunteerName { get; set; } = "";

        public string StaffName { get; set; } = "";

        public string? AssignmentStatus { get; set; }

        public int AttendanceCount { get; set; }

        public int VerifiedAttendanceCount { get; set; }

        public decimal TotalHours { get; set; }

        public bool HasAttendance { get; set; }

        public bool AllAttendanceVerified { get; set; }

        public bool AssignmentCompleted { get; set; }

        public bool VolunteerCompleted { get; set; }
    }
}