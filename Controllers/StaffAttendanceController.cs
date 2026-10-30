using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class StaffAttendanceController : Controller
    {
        private readonly VolunteerHubContext _context;

        public StaffAttendanceController(VolunteerHubContext context)
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


            var projects = _context.ProjectAssignments
                .Where(pa =>
                    pa.Sid == staff.Sid &&
                    pa.Status != "DECLINED")
                .Select(pa => pa.Project)
                .Distinct()
                .OrderByDescending(p => p.StartDate)
                .ToList();


            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;

            return View(
                "~/Views/Staff/Attendance/Index.cshtml",
                projects
            );
        }



        [HttpGet]
        public IActionResult Record(int id)
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


            var volunteers = _context.ProjectAssignments
                .Include(pa => pa.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Where(pa =>
                    pa.ProjectId == id &&
                    pa.Sid == staff.Sid &&
                    pa.Status != "DECLINED")
                .Select(pa => pa.VidNavigation)
                .OrderBy(v => v.PidNavigation.FirstName)
                .ThenBy(v => v.PidNavigation.LastName)
                .ToList();


            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;


            ViewBag.Project = project;
            ViewBag.Volunteers = volunteers;


            return View(
                "~/Views/Staff/Attendance/Record.cshtml"
            );
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Record(
            int projectId,
            List<int> volunteerIds,
            DateOnly attDate,
            string status,
            decimal? hoursWorked)
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


            volunteerIds = volunteerIds?
                .Distinct()
                .ToList()
                ?? new List<int>();

            if (!volunteerIds.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Please select at least one volunteer."
                );
            }


            var validVolunteerIds = _context.ProjectAssignments
                .Where(pa =>
                    pa.ProjectId == projectId &&
                    pa.Sid == staff.Sid &&
                    pa.Status != "DECLINED" &&
                    volunteerIds.Contains(pa.Vid))
                .Select(pa => pa.Vid)
                .ToList();

            if (validVolunteerIds.Count != volunteerIds.Count)
            {
                ModelState.AddModelError(
                    "",
                    "One or more selected volunteers are not assigned to you for this project."
                );
            }


            if (hoursWorked.HasValue &&
                (hoursWorked.Value < 0 || hoursWorked.Value > 24))
            {
                ModelState.AddModelError(
                    "",
                    "Hours worked must be between 0 and 24."
                );
            }


            if (!ModelState.IsValid)
            {
                var volunteers = _context.ProjectAssignments
                    .Include(pa => pa.VidNavigation)
                        .ThenInclude(v => v.PidNavigation)
                    .Where(pa =>
                        pa.ProjectId == projectId &&
                        pa.Sid == staff.Sid &&
                        pa.Status != "DECLINED")
                    .Select(pa => pa.VidNavigation)
                    .OrderBy(v => v.PidNavigation.FirstName)
                    .ThenBy(v => v.PidNavigation.LastName)
                    .ToList();

                ViewBag.Project = project;
                ViewBag.Volunteers = volunteers;

                ViewBag.FirstName = person.FirstName;
                ViewBag.LastName = person.LastName;
                ViewBag.Sid = staff.Sid;

                ViewBag.SelectedVolunteerIds = volunteerIds;

                return View(
                    "~/Views/Staff/Attendance/Record.cshtml"
                );
            }


            foreach (var vid in validVolunteerIds)
            {

                var existingAttendance = _context.Attendances
                    .FirstOrDefault(a =>
                        a.Vid == vid &&
                        a.ProjectId == projectId &&
                        a.AttDate == attDate);


                if (existingAttendance != null)
                {
                    existingAttendance.Status = status;
                    existingAttendance.HoursWorked = hoursWorked;

                    existingAttendance.Verified = false;

                    existingAttendance.Sid = staff.Sid;
                }


                else
                {
                    var attendance = new Attendance
                    {
                        Vid = vid,

                        ProjectId = projectId,

                        AttDate = attDate,

                        Status = status,

                        HoursWorked = hoursWorked,

                        Verified = false,

                        Sid = staff.Sid
                    };

                    _context.Attendances.Add(attendance);
                }
            }


            _context.SaveChanges();


            return RedirectToAction(nameof(Index));
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


            var assignments = _context.ProjectAssignments
                .Include(pa => pa.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .Where(pa =>
                    pa.ProjectId == id &&
                    pa.Sid == staff.Sid &&
                    pa.Status != "DECLINED")
                .OrderBy(pa => pa.VidNavigation.PidNavigation.FirstName)
                .ThenBy(pa => pa.VidNavigation.PidNavigation.LastName)
                .ToList();


            var allAttendance = _context.Attendances
                .Where(a => a.ProjectId == id)
                .ToList();


            var volunteerRows = new List<StaffCompletionRow>();

            foreach (var assignment in assignments)
            {
                var attendance = allAttendance
                    .Where(a => a.Vid == assignment.Vid)
                    .ToList();


                var volunteerName =
                    assignment.VidNavigation.PidNavigation.FirstName
                    + " "
                    + assignment.VidNavigation.PidNavigation.LastName;


                var hasAttendance = attendance.Any();

                var totalHours = attendance
                    .Sum(a => a.HoursWorked ?? 0);

                var attendanceCount = attendance.Count;

                var verifiedAttendanceCount = attendance
                    .Count(a => a.Verified == true);


                var allHoursVerified =
                    hasAttendance &&
                    attendance.All(a => a.Verified == true);


                var canMarkCompleted =
                    allHoursVerified &&
                    !string.Equals(
                        assignment.Status,
                        "COMPLETED",
                        StringComparison.OrdinalIgnoreCase
                    );

                volunteerRows.Add(
                    new StaffCompletionRow
                    {
                        AssignId = assignment.AssignId,
                        Vid = assignment.Vid,
                        VolunteerName = volunteerName,
                        AssignmentStatus = assignment.Status,
                        TotalHours = totalHours,
                        AttendanceCount = attendanceCount,
                        VerifiedAttendanceCount = verifiedAttendanceCount,
                        AllHoursVerified = allHoursVerified,
                        HasAttendance = hasAttendance,
                        CanMarkCompleted = canMarkCompleted
                    }
                );
            }


            var allProjectAssignments = _context.ProjectAssignments
                .Where(pa =>
                    pa.ProjectId == id &&
                    pa.Status != "DECLINED")
                .ToList();

            bool projectReadyForManager = false;

            if (allProjectAssignments.Any())
            {
                projectReadyForManager = true;

                foreach (var assignment in allProjectAssignments)
                {
                    var volunteerAttendance = allAttendance
                        .Where(a => a.Vid == assignment.Vid)
                        .ToList();

                    var volunteerComplete =
                        string.Equals(
                            assignment.Status,
                            "COMPLETED",
                            StringComparison.OrdinalIgnoreCase
                        )
                        &&
                        volunteerAttendance.Any()
                        &&
                        volunteerAttendance.All(
                            a => a.Verified == true
                        );

                    if (!volunteerComplete)
                    {
                        projectReadyForManager = false;
                        break;
                    }
                }
            }


            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;

            ViewBag.Project = project;

            ViewBag.VolunteerRows = volunteerRows;

            ViewBag.ProjectReadyForManager = projectReadyForManager;


            return View(
                "~/Views/Staff/Attendance/Completion.cshtml",
                project
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteAssignment(int id)
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
                .FirstOrDefault(pa =>
                    pa.AssignId == id &&
                    pa.Sid == staff.Sid &&
                    pa.Status != "DECLINED");

            if (assignment == null)
            {
                return NotFound();
            }


            var attendance = _context.Attendances
                .Where(a =>
                    a.ProjectId == assignment.ProjectId &&
                    a.Vid == assignment.Vid)
                .ToList();


            if (!attendance.Any())
            {
                TempData["ErrorMessage"] =
                    "This volunteer cannot be marked as completed because no attendance has been recorded.";

                return RedirectToAction(
                    nameof(Completion),
                    new { id = assignment.ProjectId }
                );
            }


            var allVerified = attendance
                .All(a => a.Verified == true);

            if (!allVerified)
            {
                TempData["ErrorMessage"] =
                    "This volunteer cannot be marked as completed because not all attendance hours have been verified.";

                return RedirectToAction(
                    nameof(Completion),
                    new { id = assignment.ProjectId }
                );
            }


            assignment.Status = "COMPLETED";


            _context.SaveChanges();


            TempData["SuccessMessage"] =
                "The volunteer has been marked as COMPLETED successfully.";


            return RedirectToAction(
                nameof(Completion),
                new { id = assignment.ProjectId }
            );
        }
    }


    public class StaffCompletionRow
    {
        public int AssignId { get; set; }

        public int Vid { get; set; }

        public string VolunteerName { get; set; } = "";

        public string? AssignmentStatus { get; set; }

        public decimal TotalHours { get; set; }

        public int AttendanceCount { get; set; }

        public int VerifiedAttendanceCount { get; set; }

        public bool AllHoursVerified { get; set; }

        public bool HasAttendance { get; set; }

        public bool CanMarkCompleted { get; set; }
    }
}