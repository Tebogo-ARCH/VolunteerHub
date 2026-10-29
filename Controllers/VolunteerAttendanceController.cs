using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;
using VolunteerHub.Models.ViewModels;

namespace VolunteerHub.Controllers
{
    public class VolunteerAttendanceController : Controller
    {
        private readonly VolunteerHubContext _context;

        public VolunteerAttendanceController(VolunteerHubContext context)
        {
            _context = context;
        }

        // ==========================================================
        // MY ATTENDANCE
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
            // FIND VOLUNTEER
            // ======================================================

            var volunteer = _context.Volunteers
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }


            // ======================================================
            // GET ATTENDANCE FOR THIS VOLUNTEER ONLY
            // ======================================================

            var attendance = _context.Attendances
                .Include(a => a.Project)
                .Include(a => a.SidNavigation)
                    .ThenInclude(s => s.PidNavigation)
                .Where(a => a.Vid == volunteer.Vid)
                .OrderByDescending(a => a.AttDate)
                .Select(a => new VolunteerAttendanceViewModel
                {
                    AttId = a.AttId,

                    ProjectId = a.ProjectId,

                    ProjectName = a.Project.ProjectName,

                    AttendanceDate = a.AttDate,

                    Status = a.Status,

                    HoursWorked = a.HoursWorked,

                    Verified = a.Verified ?? false,

                    StaffName =
                        a.SidNavigation.PidNavigation.FirstName
                        + " " +
                        a.SidNavigation.PidNavigation.LastName
                })
                .ToList();


            // ======================================================
            // VOLUNTEER INFORMATION
            // ======================================================

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Vid = volunteer.Vid;


            // ======================================================
            // RETURN VIEW
            // ======================================================

            return View(
                "~/Views/Volunteer/Attendance/Index.cshtml",
                attendance
            );
        }


        // ==========================================================
        // VERIFY HOURS
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyHours(int id)
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
            // FIND VOLUNTEER
            // ======================================================

            var volunteer = _context.Volunteers
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }


            // ======================================================
            // FIND ATTENDANCE
            // ======================================================
            //
            // IMPORTANT:
            // We check BOTH:
            //
            // AttId
            // AND
            // Vid
            //
            // This prevents a volunteer from verifying
            // another volunteer's attendance.
            // ======================================================

            var attendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.AttId == id &&
                    a.Vid == volunteer.Vid
                );

            if (attendance == null)
            {
                return NotFound();
            }


            // ======================================================
            // VERIFY HOURS
            // ======================================================

            attendance.Verified = true;


            // ======================================================
            // SAVE
            // ======================================================

            _context.SaveChanges();


            // ======================================================
            // SUCCESS MESSAGE
            // ======================================================

            TempData["SuccessMessage"] =
                "Your attendance hours have been verified successfully.";


            // ======================================================
            // RETURN TO ATTENDANCE
            // ======================================================

            return RedirectToAction(nameof(Index));
        }
    }
}