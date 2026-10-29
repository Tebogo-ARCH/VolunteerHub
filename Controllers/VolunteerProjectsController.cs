
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;
using VolunteerHub.Models.ViewModels;

namespace VolunteerHub.Controllers
{
    public class VolunteerProjectsController : Controller
    {
        private readonly VolunteerHubContext _context;

        public VolunteerProjectsController(VolunteerHubContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET LOGGED-IN VOLUNTEER
        // ==========================================
        private (Person? Person, Volunteer? Volunteer) GetCurrentVolunteer()
        {
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrWhiteSpace(username))
                return (null, null);

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
                return (null, null);

            var volunteer = _context.Volunteers
                .FirstOrDefault(v => v.Pid == person.Pid);

            return (person, volunteer);
        }

        // ==========================================
        // CHECK LOGIN
        // ==========================================
        private IActionResult? CheckLogin()
        {
            if (string.IsNullOrWhiteSpace(
                HttpContext.Session.GetString("Username")))
            {
                return RedirectToAction("Login", "Account");
            }

            return null;
        }

        // ==========================================
        // MY PROJECTS - CATEGORY CARDS
        // ==========================================
        [HttpGet]
        public IActionResult Index()
        {
            var loginCheck = CheckLogin();

            if (loginCheck != null)
                return loginCheck;

            var (person, volunteer) = GetCurrentVolunteer();

            if (person == null)
                return RedirectToAction("Login", "Account");

            if (volunteer == null)
                return Forbid();

            SetVolunteerInformation(person, volunteer);

            return View(
                "~/Views/Volunteer/Projects/Index.cshtml",
                new List<VolunteerProjectViewModel>()
            );
        }

        // ==========================================
        // UPCOMING PROJECTS
        // ==========================================
        [HttpGet]
        public IActionResult Upcoming()
        {
            var loginCheck = CheckLogin();

            if (loginCheck != null)
                return loginCheck;

            var (person, volunteer) = GetCurrentVolunteer();

            if (person == null)
                return RedirectToAction("Login", "Account");

            if (volunteer == null)
                return Forbid();

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Upcoming projects belong to the volunteer's organization,
            // have PLANNING status, and have not started yet.
            var projects = _context.Projects
                .Include(p => p.MidNavigation)
                    .ThenInclude(m => m.Organizations)
                .Where(p =>
                    p.Mid == volunteer.Mid &&
                    p.Status != null &&
                    p.Status.ToUpper() == "PLANNING" &&
                    p.StartDate >= today)
                .OrderBy(p => p.StartDate)
                .ToList();

            var projectIds = projects
                .Select(p => p.ProjectId)
                .ToList();

            var assignments = _context.ProjectAssignments
                .Where(pa =>
                    pa.Vid == volunteer.Vid &&
                    projectIds.Contains(pa.ProjectId))
                .ToDictionary(pa => pa.ProjectId);

            var model = projects.Select(project =>
            {
                assignments.TryGetValue(
                    project.ProjectId,
                    out var assignment);

                return BuildViewModel(project, assignment);
            }).ToList();

            SetVolunteerInformation(person, volunteer);

            return View(
                "~/Views/Volunteer/Projects/Upcoming.cshtml",
                model
            );
        }

        // ==========================================
        // APPLY FOR AN UPCOMING PROJECT
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Apply(int projectId)
        {
            var loginCheck = CheckLogin();

            if (loginCheck != null)
                return loginCheck;

            var (person, volunteer) = GetCurrentVolunteer();

            if (person == null)
                return RedirectToAction("Login", "Account");

            if (volunteer == null)
                return Forbid();

            var today = DateOnly.FromDateTime(DateTime.Today);

            // Only allow applications for future PLANNING projects
            // belonging to the volunteer's organization.
            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == projectId &&
                    p.Mid == volunteer.Mid &&
                    p.Status != null &&
                    p.Status.ToUpper() == "PLANNING" &&
                    p.StartDate >= today);

            if (project == null)
            {
                TempData["ErrorMessage"] =
                    "This project is unavailable for applications.";

                return RedirectToAction(nameof(Upcoming));
            }

            // SID is required by PROJECT_ASSIGNMENTS.
            var staff = _context.Staff
                .Where(s => s.Mid == project.Mid)
                .OrderBy(s => s.Sid)
                .FirstOrDefault();

            if (staff == null)
            {
                TempData["ErrorMessage"] =
                    "Your organization does not currently have a staff member available to handle this application.";

                return RedirectToAction(nameof(Upcoming));
            }

            // Reuse an existing declined assignment rather than
            // inserting a duplicate project-volunteer record.
            var assignment = _context.ProjectAssignments
                .FirstOrDefault(pa =>
                    pa.ProjectId == projectId &&
                    pa.Vid == volunteer.Vid);

            if (assignment != null)
            {
                var existingStatus = assignment.Status?.ToUpper();

                if (existingStatus == "PENDING")
                {
                    TempData["InfoMessage"] =
                        "You have already applied for this project.";

                    return RedirectToAction(nameof(Upcoming));
                }

                if (existingStatus == "ACCEPTED" ||
                    existingStatus == "ASSIGNED")
                {
                    TempData["InfoMessage"] =
                        "You are already involved in this project.";

                    return RedirectToAction(nameof(Upcoming));
                }

                if (existingStatus != "DECLINED" &&
                    existingStatus != "DECLINE")
                {
                    TempData["ErrorMessage"] =
                        "This project already has an assignment record for you.";

                    return RedirectToAction(nameof(Upcoming));
                }

                // Permit reapplication after a previous decline.
                assignment.Sid = staff.Sid;
                assignment.Status = "PENDING";
            }
            else
            {
                assignment = new ProjectAssignment
                {
                    ProjectId = projectId,
                    Vid = volunteer.Vid,
                    Sid = staff.Sid,
                    Status = "PENDING"
                };

                _context.ProjectAssignments.Add(assignment);
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] =
                "Your application has been submitted successfully. Please wait for staff to review it.";

            return RedirectToAction(nameof(Upcoming));
        }

        // ==========================================
        // ONGOING PROJECTS
        // ==========================================
        [HttpGet]
        public IActionResult Ongoing()
        {
            var loginCheck = CheckLogin();

            if (loginCheck != null)
                return loginCheck;

            var (person, volunteer) = GetCurrentVolunteer();

            if (person == null)
                return RedirectToAction("Login", "Account");

            if (volunteer == null)
                return Forbid();

            // Supports both status values used for active projects.
            var model = GetVolunteerProjects(
                volunteer.Vid,
                volunteer.Mid,
                "ONGOING");

            SetVolunteerInformation(person, volunteer);

            return View(
                "~/Views/Volunteer/Projects/Ongoing.cshtml",
                model
            );
        }

        // ==========================================
        // COMPLETED PROJECTS
        // ==========================================
        [HttpGet]
        public IActionResult Completed()
        {
            var loginCheck = CheckLogin();

            if (loginCheck != null)
                return loginCheck;

            var (person, volunteer) = GetCurrentVolunteer();

            if (person == null)
                return RedirectToAction("Login", "Account");

            if (volunteer == null)
                return Forbid();

            var model = GetVolunteerProjects(
                volunteer.Vid,
                volunteer.Mid,
                "COMPLETED");

            SetVolunteerInformation(person, volunteer);

            return View(
                "~/Views/Volunteer/Projects/Completed.cshtml",
                model
            );
        }

        // ==========================================
        // CANCELED PROJECTS
        // ==========================================
        [HttpGet]
        public IActionResult Canceled()
        {
            var loginCheck = CheckLogin();

            if (loginCheck != null)
                return loginCheck;

            var (person, volunteer) = GetCurrentVolunteer();

            if (person == null)
                return RedirectToAction("Login", "Account");

            if (volunteer == null)
                return Forbid();

            // Includes canceled projects for which this volunteer
            // has a pending application or a non-declined assignment.
            var model = GetVolunteerProjects(
                volunteer.Vid,
                volunteer.Mid,
                "CANCELED");

            SetVolunteerInformation(person, volunteer);

            return View(
                "~/Views/Volunteer/Projects/Canceled.cshtml",
                model
            );
        }

        // ==========================================
        // GET PROJECTS FOR A VOLUNTEER CATEGORY
        // ==========================================
        private List<VolunteerProjectViewModel> GetVolunteerProjects(
            int volunteerId,
            int managerId,
            string projectStatus)
        {
            // Recognize alternative status spellings.
            string[] allowedProjectStatuses =
                projectStatus.ToUpperInvariant() switch
                {
                    "ONGOING" => new[]
                    {
                        "ACTIVE",
                        "ONGOING"
                    },

                    "COMPLETED" => new[]
                    {
                        "COMPLETED"
                    },

                    "CANCELED" => new[]
                    {
                        "CANCELED",
                        "CANCELLED"
                    },

                    _ => new[]
                    {
                        projectStatus.ToUpperInvariant()
                    }
                };

            // Ongoing projects require an accepted or assigned
            // volunteer. Completed projects can also have a
            // COMPLETED assignment status. Canceled projects include
            // pending applications and non-declined assignments.
            string[] allowedAssignmentStatuses =
                projectStatus.ToUpperInvariant() switch
                {
                    "COMPLETED" => new[]
                    {
                        "ACCEPTED",
                        "ASSIGNED",
                        "COMPLETED"
                    },

                    "CANCELED" => new[]
                    {
                        "PENDING",
                        "ACCEPTED",
                        "ASSIGNED",
                        "COMPLETED"
                    },

                    _ => new[]
                    {
                        "ACCEPTED",
                        "ASSIGNED"
                    }
                };

            return _context.ProjectAssignments
                .Include(pa => pa.Project)
                    .ThenInclude(p => p.MidNavigation)
                        .ThenInclude(m => m.Organizations)
                .Where(pa =>
                    pa.Vid == volunteerId &&
                    pa.Project.Mid == managerId &&
                    pa.Project.Status != null &&
                    allowedProjectStatuses.Contains(
                        pa.Project.Status.ToUpper()) &&
                    pa.Status != null &&
                    allowedAssignmentStatuses.Contains(
                        pa.Status.ToUpper()))
                .OrderByDescending(pa => pa.Project.StartDate)
                .Select(pa => new VolunteerProjectViewModel
                {
                    AssignId = pa.AssignId,
                    ProjectId = pa.ProjectId,
                    ProjectName = pa.Project.ProjectName,
                    Description = pa.Project.Description,
                    StartDate = pa.Project.StartDate,
                    EndDate = pa.Project.EndDate,
                    Status = pa.Project.Status,
                    Location = pa.Project.Location,
                    AssignmentStatus = pa.Status,

                    OrganizationName = pa.Project.MidNavigation
                        .Organizations
                        .Select(o => o.OrgName)
                        .FirstOrDefault()
                })
                .ToList();
        }

        // ==========================================
        // VIEW PROJECT DETAILS
        // ==========================================
        [HttpGet]
        public IActionResult Details(int id)
        {
            var loginCheck = CheckLogin();

            if (loginCheck != null)
                return loginCheck;

            var (person, volunteer) = GetCurrentVolunteer();

            if (person == null)
                return RedirectToAction("Login", "Account");

            if (volunteer == null)
                return Forbid();

            var assignment = _context.ProjectAssignments
                .Include(pa => pa.Project)
                    .ThenInclude(p => p.MidNavigation)
                        .ThenInclude(m => m.Organizations)
                            .ThenInclude(o => o.Address)
                .FirstOrDefault(pa =>
                    pa.ProjectId == id &&
                    pa.Vid == volunteer.Vid &&
                    pa.Project.Mid == volunteer.Mid);

            if (assignment == null)
                return NotFound();

            var organization = assignment.Project
                .MidNavigation
                .Organizations
                .FirstOrDefault();

            var model = new VolunteerProjectViewModel
            {
                AssignId = assignment.AssignId,
                ProjectId = assignment.ProjectId,
                ProjectName = assignment.Project.ProjectName,
                Description = assignment.Project.Description,
                StartDate = assignment.Project.StartDate,
                EndDate = assignment.Project.EndDate,
                Status = assignment.Project.Status,
                Location = assignment.Project.Location,
                AssignmentStatus = assignment.Status,

                OrganizationName = organization?.OrgName,
                OrganizationDescription = organization?.Description,
                OrganizationEmail = organization?.Email,
                OrganizationPhone = organization?.Phone
            };

            if (organization?.Address != null)
            {
                model.OrganizationStreetAddress =
                    organization.Address.StreetAddress;

                model.OrganizationSuburb =
                    organization.Address.Suburb;

                model.OrganizationCity =
                    organization.Address.City;

                model.OrganizationProvince =
                    organization.Address.Province;

                model.OrganizationPostalCode =
                    organization.Address.PostalCode;
            }

            SetVolunteerInformation(person, volunteer);

            return View(
                "~/Views/Volunteer/Projects/Details.cshtml",
                model
            );
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================
        private void SetVolunteerInformation(
            Person person,
            Volunteer volunteer)
        {
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Vid = volunteer.Vid;
        }

        private static VolunteerProjectViewModel BuildViewModel(
            Project project,
            ProjectAssignment? assignment)
        {
            return new VolunteerProjectViewModel
            {
                AssignId = assignment?.AssignId ?? 0,
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status,
                Location = project.Location,
                AssignmentStatus = assignment?.Status,

                OrganizationName = project.MidNavigation
                    .Organizations
                    .Select(o => o.OrgName)
                    .FirstOrDefault()
            };
        }
    }
}
