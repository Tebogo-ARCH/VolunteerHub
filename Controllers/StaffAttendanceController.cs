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

        // ==========================================================
        // PROJECTS
        // ==========================================================

        [HttpGet]
        public IActionResult Index()
        {
            // ======================================================
            // CHECK LOGIN
            // ======================================================

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // ======================================================
            // FIND PERSON
            // ======================================================

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ======================================================
            // FIND STAFF
            // ======================================================

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            // ======================================================
            // GET PROJECTS ASSIGNED TO THIS STAFF MEMBER
            // ======================================================
            //
            // SID in PROJECT_ASSIGNMENTS identifies the staff member
            // supervising the volunteer.
            //
            // Therefore, a staff member should only see projects
            // where they actually have volunteer assignments.
            // ======================================================

            var projects = _context.ProjectAssignments
                .Where(pa =>
                    pa.Sid == staff.Sid &&
                    pa.Status != "DECLINED")
                .Select(pa => pa.Project)
                .Distinct()
                .OrderByDescending(p => p.StartDate)
                .ToList();

            // ======================================================
            // STAFF INFORMATION
            // ======================================================

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;

            return View(
                "~/Views/Staff/Attendance/Index.cshtml",
                projects
            );
        }


        // ==========================================================
        // RECORD ATTENDANCE - GET
        // ==========================================================

        [HttpGet]
        public IActionResult Record(int id)
        {
            // ======================================================
            // CHECK LOGIN
            // ======================================================

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // ======================================================
            // FIND PERSON
            // ======================================================

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ======================================================
            // FIND STAFF
            // ======================================================

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            // ======================================================
            // FIND PROJECT
            // ======================================================
            //
            // The project must belong to the staff member's manager.
            // ======================================================

            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.Mid == staff.Mid);

            if (project == null)
            {
                return NotFound();
            }

            // ======================================================
            // GET VOLUNTEERS ASSIGNED TO THIS STAFF MEMBER
            // ======================================================

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

            // ======================================================
            // STAFF INFORMATION
            // ======================================================

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;

            // ======================================================
            // PAGE INFORMATION
            // ======================================================

            ViewBag.Project = project;
            ViewBag.Volunteers = volunteers;

            // ======================================================
            // RETURN RECORD ATTENDANCE PAGE
            // ======================================================

            return View(
                "~/Views/Staff/Attendance/Record.cshtml"
            );
        }


        // ==========================================================
        // RECORD ATTENDANCE - POST
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Record(
            int projectId,
            List<int> volunteerIds,
            DateOnly attDate,
            string status,
            decimal? hoursWorked)
        {
            // ======================================================
            // CHECK LOGIN
            // ======================================================

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // ======================================================
            // FIND PERSON
            // ======================================================

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ======================================================
            // FIND STAFF
            // ======================================================

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            // ======================================================
            // FIND PROJECT
            // ======================================================

            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == projectId &&
                    p.Mid == staff.Mid);

            if (project == null)
            {
                return NotFound();
            }

            // ======================================================
            // VALIDATE VOLUNTEERS
            // ======================================================

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

            // ======================================================
            // ONLY ALLOW VOLUNTEERS ASSIGNED TO THIS STAFF MEMBER
            // ======================================================

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

            // ======================================================
            // VALIDATE HOURS
            // ======================================================

            if (hoursWorked.HasValue &&
                (hoursWorked.Value < 0 || hoursWorked.Value > 24))
            {
                ModelState.AddModelError(
                    "",
                    "Hours worked must be between 0 and 24."
                );
            }

            // ======================================================
            // IF VALIDATION FAILED
            // ======================================================

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

            // ======================================================
            // RECORD ATTENDANCE FOR EACH SELECTED VOLUNTEER
            // ======================================================

            foreach (var vid in validVolunteerIds)
            {
                // ==================================================
                // CHECK WHETHER ATTENDANCE ALREADY EXISTS
                // ==================================================

                var existingAttendance = _context.Attendances
                    .FirstOrDefault(a =>
                        a.Vid == vid &&
                        a.ProjectId == projectId &&
                        a.AttDate == attDate);

                // ==================================================
                // UPDATE EXISTING RECORD
                // ==================================================

                if (existingAttendance != null)
                {
                    existingAttendance.Status = status;
                    existingAttendance.HoursWorked = hoursWorked;

                    // If staff changes the attendance,
                    // volunteer must verify it again.
                    existingAttendance.Verified = false;

                    existingAttendance.Sid = staff.Sid;
                }

                // ==================================================
                // CREATE NEW RECORD
                // ==================================================

                else
                {
                    var attendance = new Attendance
                    {
                        Vid = vid,

                        ProjectId = projectId,

                        AttDate = attDate,

                        Status = status,

                        HoursWorked = hoursWorked,

                        // Volunteer must verify later.
                        Verified = false,

                        // Staff member who recorded the hours.
                        Sid = staff.Sid
                    };

                    _context.Attendances.Add(attendance);
                }
            }

            // ======================================================
            // SAVE
            // ======================================================

            _context.SaveChanges();

            // ======================================================
            // RETURN TO PROJECT LIST
            // ======================================================

            return RedirectToAction(nameof(Index));
        }


        // ==========================================================
        // COMPLETION PAGE - GET
        // ==========================================================
        //
        // Shows the staff member:
        // - Volunteers assigned to them
        // - Assignment status
        // - Attendance records
        // - Total hours
        // - Verified hours
        // - Whether volunteer can be marked COMPLETED
        //
        // The project itself is NOT completed here.
        // The manager will handle project completion later.
        // ==========================================================

        [HttpGet]
        public IActionResult Completion(int id)
        {
            // ======================================================
            // CHECK LOGIN
            // ======================================================

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // ======================================================
            // FIND PERSON
            // ======================================================

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ======================================================
            // FIND STAFF
            // ======================================================

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            // ======================================================
            // FIND PROJECT
            // ======================================================

            var project = _context.Projects
                .FirstOrDefault(p =>
                    p.ProjectId == id &&
                    p.Mid == staff.Mid);

            if (project == null)
            {
                return NotFound();
            }

            // ======================================================
            // GET THIS STAFF MEMBER'S ASSIGNMENTS
            // ======================================================

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

            // ======================================================
            // GET ALL ATTENDANCE FOR THIS PROJECT
            // ======================================================

            var allAttendance = _context.Attendances
                .Where(a => a.ProjectId == id)
                .ToList();

            // ======================================================
            // BUILD COMPLETION INFORMATION
            // ======================================================

            var volunteerRows = new List<StaffCompletionRow>();

            foreach (var assignment in assignments)
            {
                var attendance = allAttendance
                    .Where(a => a.Vid == assignment.Vid)
                    .ToList();

                // ==================================================
                // VOLUNTEER NAME
                // ==================================================

                var volunteerName =
                    assignment.VidNavigation.PidNavigation.FirstName
                    + " "
                    + assignment.VidNavigation.PidNavigation.LastName;

                // ==================================================
                // ATTENDANCE INFORMATION
                // ==================================================

                var hasAttendance = attendance.Any();

                var totalHours = attendance
                    .Sum(a => a.HoursWorked ?? 0);

                var attendanceCount = attendance.Count;

                var verifiedAttendanceCount = attendance
                    .Count(a => a.Verified == true);

                // ==================================================
                // ALL HOURS VERIFIED
                // ==================================================
                //
                // A volunteer cannot be completed if:
                // - there are no attendance records
                // - any attendance record is not verified
                // ==================================================

                var allHoursVerified =
                    hasAttendance &&
                    attendance.All(a => a.Verified == true);

                // ==================================================
                // CAN MARK COMPLETED
                // ==================================================

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

            // ======================================================
            // DETERMINE WHETHER THE WHOLE PROJECT IS READY
            // ======================================================
            //
            // This checks ALL non-declined assignments for the
            // project, not only this staff member's assignments.
            //
            // This is important because a project may have multiple
            // staff members supervising different volunteers.
            // ======================================================

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

            // ======================================================
            // PAGE INFORMATION
            // ======================================================

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Sid = staff.Sid;

            ViewBag.Project = project;

            ViewBag.VolunteerRows = volunteerRows;

            ViewBag.ProjectReadyForManager = projectReadyForManager;

            // ======================================================
            // RETURN COMPLETION PAGE
            // ======================================================
            //
            // IMPORTANT:
            // Completion.cshtml declares:
            //
            // @model VolunteerHub.Models.Project
            //
            // Therefore we MUST pass "project" as the model.
            // ======================================================

            return View(
                "~/Views/Staff/Attendance/Completion.cshtml",
                project
            );
        }


        // ==========================================================
        // COMPLETE VOLUNTEER ASSIGNMENT - POST
        // ==========================================================
        //
        // A staff member can only mark a volunteer completed when:
        //
        // 1. The assignment belongs to that staff member.
        // 2. The assignment is not DECLINED.
        // 3. Attendance exists.
        // 4. Every attendance record is VERIFIED.
        //
        // The project itself is NOT changed to COMPLETED here.
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteAssignment(int id)
        {
            // ======================================================
            // CHECK LOGIN
            // ======================================================

            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // ======================================================
            // FIND PERSON
            // ======================================================

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ======================================================
            // FIND STAFF
            // ======================================================

            var staff = _context.Staff
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return RedirectToAction("Index", "Staff");
            }

            // ======================================================
            // FIND ASSIGNMENT
            // ======================================================
            //
            // SECURITY:
            // The assignment must belong to the logged-in staff
            // member through SID.
            // ======================================================

            var assignment = _context.ProjectAssignments
                .FirstOrDefault(pa =>
                    pa.AssignId == id &&
                    pa.Sid == staff.Sid &&
                    pa.Status != "DECLINED");

            if (assignment == null)
            {
                return NotFound();
            }

            // ======================================================
            // GET ATTENDANCE
            // ======================================================

            var attendance = _context.Attendances
                .Where(a =>
                    a.ProjectId == assignment.ProjectId &&
                    a.Vid == assignment.Vid)
                .ToList();

            // ======================================================
            // REQUIRE ATTENDANCE
            // ======================================================

            if (!attendance.Any())
            {
                TempData["ErrorMessage"] =
                    "This volunteer cannot be marked as completed because no attendance has been recorded.";

                return RedirectToAction(
                    nameof(Completion),
                    new { id = assignment.ProjectId }
                );
            }

            // ======================================================
            // REQUIRE ALL ATTENDANCE TO BE VERIFIED
            // ======================================================

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

            // ======================================================
            // MARK ASSIGNMENT COMPLETED
            // ======================================================

            assignment.Status = "COMPLETED";

            // ======================================================
            // SAVE
            // ======================================================

            _context.SaveChanges();

            // ======================================================
            // SUCCESS MESSAGE
            // ======================================================

            TempData["SuccessMessage"] =
                "The volunteer has been marked as COMPLETED successfully.";

            // ======================================================
            // RETURN TO COMPLETION PAGE
            // ======================================================

            return RedirectToAction(
                nameof(Completion),
                new { id = assignment.ProjectId }
            );
        }
    }


    // ==============================================================
    // COMPLETION ROW
    // ==============================================================
    //
    // This small class is used only by the Completion page.
    // It avoids creating another ViewModel file for now.
    // ==============================================================

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