
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class VolunteerCommunicationController : Controller
    {
        private readonly VolunteerHubContext _context;

        private const string IndexView =
            "~/Views/Volunteer/VolunteerCommunication/Index.cshtml";

        private const string DetailsView =
            "~/Views/Volunteer/VolunteerCommunication/Details.cshtml";

        public VolunteerCommunicationController(
            VolunteerHubContext context)
        {
            _context = context;
        }

        private (
            Person? Person,
            Volunteer? Volunteer,
            Organization? Organization
        ) GetVolunteerDetails()
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

            var volunteer = _context.Set<Volunteer>()
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return (person, null, null);
            }

            var organization = _context.Organizations
                .FirstOrDefault(o => o.Mid == volunteer.Mid);

            return (person, volunteer, organization);
        }

        [HttpGet]
        public IActionResult Index()
        {
            var details = GetVolunteerDetails();

            if (details.Person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (details.Volunteer == null)
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
            var details = GetVolunteerDetails();

            if (details.Person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (details.Volunteer == null)
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
