using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;
using VolunteerHub.Models.ViewModels;

namespace VolunteerHub.Controllers
{
    public class StaffProjectsController : Controller
    {
        private readonly VolunteerHubContext _context;

        public StaffProjectsController(VolunteerHubContext context)
        {
            _context = context;
        }

        // ==========================================================
        // PROJECT LIST
        // ==========================================================
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

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            // Get projects managed by this staff member's organization.
            var projects = _context.Projects
                .Where(p => p.Mid == staff.Mid)
                .OrderByDescending(p => p.StartDate)
                .ToList();

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Sid = staff.Sid;

            return View("/Views/Staff/Projects/Index.cshtml", projects);
        }

        // ==========================================================
        // ASSIGN VOLUNTEERS - GET
        // ==========================================================
        [HttpGet]
        public IActionResult Assign(int id)
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

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            // Confirm the project belongs to this staff member's organization.
            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.Mid == staff.Mid);

            if (project == null)
            {
                return NotFound();
            }

            // Get volunteers from the same organization and their skills.
            var volunteers = _context.Volunteers
                .Include(v => v.PidNavigation)
                .Include(v => v.VolunteerSkills)
                    .ThenInclude(vs => vs.Skill)
                .Where(v => v.Mid == staff.Mid)
                .OrderBy(v => v.PidNavigation.FirstName)
                .ThenBy(v => v.PidNavigation.LastName)
                .ToList();

            // Include all existing assignment records to avoid duplicates.
            var assignedVolunteerIds = _context.ProjectAssignments
                .Where(pa => pa.ProjectId == id)
                .Select(pa => pa.Vid)
                .ToList();

            var model = new StaffProjectAssignmentViewModel
            {
                Project = project,
                Volunteers = volunteers,
                AssignedVolunteerIds = assignedVolunteerIds,
                Sid = staff.Sid,
                FirstName = person.FirstName,
                LastName = person.LastName,
                UserName = person.UserName
            };

            return View("/Views/Staff/Projects/Assign.cshtml", model);
        }

        // ==========================================================
        // ASSIGN VOLUNTEERS - POST
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Assign(
            int projectId,
            List<int> volunteerIds)
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

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == projectId &&
                    p.Mid == staff.Mid);

            if (project == null)
            {
                return NotFound();
            }

            if (volunteerIds == null || !volunteerIds.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one volunteer.");
            }

            // Remove duplicate volunteer IDs from the submitted list.
            volunteerIds = volunteerIds?
                .Distinct()
                .ToList() ?? new List<int>();

            // Only accept volunteers who belong to this organization.
            var validVolunteerIds = _context.Volunteers
                .Where(v =>
                    v.Mid == staff.Mid &&
                    volunteerIds.Contains(v.Vid))
                .Select(v => v.Vid)
                .ToList();

            if (validVolunteerIds.Count != volunteerIds.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected volunteers do not belong to your organization.");
            }

            // If validation fails, reload the assignment page.
            if (!ModelState.IsValid)
            {
                var volunteers = _context.Volunteers
                    .Include(v => v.PidNavigation)
                    .Include(v => v.VolunteerSkills)
                        .ThenInclude(vs => vs.Skill)
                    .Where(v => v.Mid == staff.Mid)
                    .OrderBy(v => v.PidNavigation.FirstName)
                    .ThenBy(v => v.PidNavigation.LastName)
                    .ToList();

                var assignedVolunteerIds = _context.ProjectAssignments
                    .Where(pa => pa.ProjectId == projectId)
                    .Select(pa => pa.Vid)
                    .ToList();

                var model = new StaffProjectAssignmentViewModel
                {
                    Project = project,
                    Volunteers = volunteers,
                    AssignedVolunteerIds = assignedVolunteerIds,
                    SelectedVolunteerIds = volunteerIds,
                    Sid = staff.Sid,
                    FirstName = person.FirstName,
                    LastName = person.LastName,
                    UserName = person.UserName
                };

                return View("/Views/Staff/Projects/Assign.cshtml", model);
            }

            // Get volunteers who already have assignment records.
            var existingAssignments = _context.ProjectAssignments
                .Where(pa =>
                    pa.ProjectId == projectId &&
                    volunteerIds.Contains(pa.Vid))
                .ToList();

            var alreadyAssignedIds = existingAssignments
                .Select(pa => pa.Vid)
                .ToHashSet();

            // Preserve the existing manual-assignment workflow.
            foreach (var vid in validVolunteerIds)
            {
                if (alreadyAssignedIds.Contains(vid))
                {
                    continue;
                }

                var assignment = new ProjectAssignment
                {
                    ProjectId = projectId,
                    Vid = vid,
                    Sid = staff.Sid,
                    Status = "ASSIGNED"
                };

                _context.ProjectAssignments.Add(assignment);
            }

            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // ==========================================================
        // REVIEW VOLUNTEER APPLICATIONS - GET
        // ==========================================================
        [HttpGet]
        public IActionResult Requests()
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

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            // Only show pending applications for this organization.
            var requests = _context.ProjectAssignments
                .Include(pa => pa.Project)
                .Include(pa => pa.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Where(pa =>
                    pa.Project.Mid == staff.Mid &&
                    pa.Status != null &&
                    pa.Status.ToUpper() == "PENDING")
                .OrderBy(pa => pa.Project.StartDate)
                .ToList();

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;

            return View("/Views/Staff/Projects/Requests.cshtml", requests);
        }

        // ==========================================================
        // ACCEPT VOLUNTEER APPLICATION
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptRequest(int id)
        {
            return ReviewApplication(id, "ACCEPTED");
        }

        // ==========================================================
        // DECLINE VOLUNTEER APPLICATION
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeclineRequest(int id)
        {
            return ReviewApplication(id, "DECLINED");
        }

        // ==========================================================
        // PROCESS APPLICATION REVIEW
        // ==========================================================
        private IActionResult ReviewApplication(
            int id,
            string newStatus)
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

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            // Only allow staff to review pending requests for their organization.
            var assignment = _context.ProjectAssignments
                .Include(pa => pa.Project)
                .FirstOrDefault(pa =>
                    pa.AssignId == id &&
                    pa.Project.Mid == staff.Mid &&
                    pa.Status != null &&
                    pa.Status.ToUpper() == "PENDING");

            if (assignment == null)
            {
                TempData["ErrorMessage"] =
                    "The application could not be found, or it has already been reviewed.";

                return RedirectToAction(nameof(Requests));
            }

            assignment.Status = newStatus;

            // Record which staff member reviewed the request.
            assignment.Sid = staff.Sid;

            _context.SaveChanges();

            TempData["SuccessMessage"] =
                newStatus == "ACCEPTED"
                    ? "The volunteer application has been accepted."
                    : "The volunteer application has been declined.";

            return RedirectToAction(nameof(Requests));
        }
    }
}