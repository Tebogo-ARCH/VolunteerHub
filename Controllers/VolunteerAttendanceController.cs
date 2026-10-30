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

            var volunteer = _context.Volunteers
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }

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

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Vid = volunteer.Vid;

            return View(
                "~/Views/Volunteer/Attendance/Index.cshtml",
                attendance
            );
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyHours(int id)
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


            var volunteer = _context.Volunteers
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }

            var attendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.AttId == id &&
                    a.Vid == volunteer.Vid
                );

            if (attendance == null)
            {
                return NotFound();
            }


            attendance.Verified = true;
            _context.SaveChanges();


            TempData["SuccessMessage"] =
                "Your attendance hours have been verified successfully.";


            return RedirectToAction(nameof(Index));
        }
    }
}