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

            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.Mid == staff.Mid);

            if (project == null)
            {
                return NotFound();
            }

            var volunteers = _context.Volunteers
                .Include(v => v.PidNavigation)
                .Include(v => v.VolunteerSkills)
                    .ThenInclude(vs => vs.Skill)
                .Where(v => v.Mid == staff.Mid)
                .OrderBy(v => v.PidNavigation.FirstName)
                .ThenBy(v => v.PidNavigation.LastName)
                .ToList();

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

            volunteerIds = volunteerIds?
                .Distinct()
                .ToList() ?? new List<int>();

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

            var existingAssignments = _context.ProjectAssignments
                .Where(pa =>
                    pa.ProjectId == projectId &&
                    volunteerIds.Contains(pa.Vid))
                .ToList();

            var alreadyAssignedIds = existingAssignments
                .Select(pa => pa.Vid)
                .ToHashSet();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptRequest(int id)
        {
            return ReviewApplication(id, "ACCEPTED");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeclineRequest(int id)
        {
            return ReviewApplication(id, "DECLINED");
        }

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