
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class StaffCommunicationController : Controller
    {
        private readonly VolunteerHubContext _context;

        private const string IndexView =
            "~/Views/Staff/StaffCommunication/Index.cshtml";

        private const string DetailsView =
            "~/Views/Staff/StaffCommunication/Details.cshtml";

        public StaffCommunicationController(
            VolunteerHubContext context)
        {
            _context = context;
        }

        private (
            Person? Person,
            Staff? Staff,
            Organization? Organization
        ) GetStaffDetails()
        {
            var username = HttpContext.Session.GetString("Username");

            if (string.IsNullOrWhiteSpace(username))
            {
                return (null, null, null);
            }

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            if (person == null)
            {
                return (null, null, null);
            }

            var staff = _context.Set<Staff>()
                .FirstOrDefault(s => s.Pid == person.Pid);

            if (staff == null)
            {
                return (person, null, null);
            }

            var organization = _context.Organizations
                .FirstOrDefault(o => o.Mid == staff.Mid);

            return (person, staff, organization);
        }

        [HttpGet]
        public IActionResult Index()
        {
            var details = GetStaffDetails();

            if (details.Person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (details.Staff == null)
            {
                return Forbid();
            }

            if (details.Organization == null)
            {
                TempData["ErrorMessage"] =
                    "No organization is assigned to your account.";

                return View(IndexView,
                    new System.Collections.Generic.List<Announcement>());
            }

            var announcements = _context.Announcements
                .Where(a => a.Oid == details.Organization.Oid)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            ViewBag.FirstName = details.Person.FirstName;
            ViewBag.LastName = details.Person.LastName;
            ViewBag.OrganizationName = details.Organization.OrgName;

            return View(IndexView, announcements);
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var details = GetStaffDetails();

            if (details.Person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (details.Staff == null)
            {
                return Forbid();
            }

            if (details.Organization == null)
            {
                return NotFound();
            }

            var announcement = _context.Announcements
                .FirstOrDefault(a =>
                    a.AnnId == id &&
                    a.Oid == details.Organization.Oid);

            if (announcement == null)
            {
                return NotFound();
            }

            ViewBag.FirstName = details.Person.FirstName;
            ViewBag.LastName = details.Person.LastName;
            ViewBag.OrganizationName = details.Organization.OrgName;

            return View(DetailsView, announcement);
        }
    }
}
