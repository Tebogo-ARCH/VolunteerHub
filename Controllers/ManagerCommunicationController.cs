
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class ManagerCommunicationController : Controller
    {
        private readonly VolunteerHubContext _context;

        private const string IndexView =
            "~/Views/Manager/ManagerCommunication/Index.cshtml";

        private const string CreateView =
            "~/Views/Manager/ManagerCommunication/Create.cshtml";

        private const string DetailsView =
            "~/Views/Manager/ManagerCommunication/Details.cshtml";

        public ManagerCommunicationController(
            VolunteerHubContext context)
        {
            _context = context;
        }

        private (
            Person? Person,
            Manager? Manager,
            Organization? Organization
        ) GetManagerDetails()
        {
            var username = HttpContext.Session
                .GetString("Username");

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

            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            if (manager == null)
            {
                return (person, null, null);
            }

            var organization = _context.Organizations
                .FirstOrDefault(o => o.Mid == manager.Mid);

            return (person, manager, organization);
        }

        [HttpGet]
        public IActionResult Index()
        {
            var details = GetManagerDetails();

            if (details.Person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (details.Manager == null)
            {
                return Forbid();
            }

            if (details.Organization == null)
            {
                TempData["ErrorMessage"] =
                    "No organization is assigned to your account.";

                return View(IndexView, new List<Announcement>());
            }

            var announcements = _context.Announcements
                .Where(a =>
                    a.Mid == details.Manager.Mid &&
                    a.Oid == details.Organization.Oid)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            ViewBag.FirstName = details.Person.FirstName;
            ViewBag.LastName = details.Person.LastName;
            ViewBag.OrganizationName =
                details.Organization.OrgName;

            return View(IndexView, announcements);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var details = GetManagerDetails();

            if (details.Person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (details.Manager == null)
            {
                return Forbid();
            }

            if (details.Organization == null)
            {
                TempData["ErrorMessage"] =
                    "You must have an assigned organization before sending communication.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.OrganizationName =
                details.Organization.OrgName;

            return View(CreateView);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string title, string content)
        {
            var details = GetManagerDetails();

            if (details.Person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (details.Manager == null)
            {
                return Forbid();
            }

            if (details.Organization == null)
            {
                TempData["ErrorMessage"] =
                    "No organization is assigned to your account.";

                return RedirectToAction(nameof(Index));
            }

            title = title?.Trim() ?? string.Empty;
            content = content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(content))
            {
                ViewBag.ErrorMessage =
                    "Please enter both an announcement title and message.";

                ViewBag.OrganizationName =
                    details.Organization.OrgName;

                ViewBag.TitleValue = title;
                ViewBag.ContentValue = content;

                return View(CreateView);
            }

            if (title.Length > 200)
            {
                ViewBag.ErrorMessage =
                    "The announcement title cannot exceed 200 characters.";

                ViewBag.OrganizationName =
                    details.Organization.OrgName;

                ViewBag.TitleValue = title;
                ViewBag.ContentValue = content;

                return View(CreateView);
            }

            var announcement = new Announcement
            {
                Title = title,
                Content = content,
                Mid = details.Manager.Mid,
                Oid = details.Organization.Oid,
                CreatedAt = DateTime.Now
            };

            _context.Announcements.Add(announcement);
            _context.SaveChanges();

            TempData["SuccessMessage"] =
                "Your announcement has been created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var details = GetManagerDetails();

            if (details.Person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (details.Manager == null)
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
                    a.Mid == details.Manager.Mid &&
                    a.Oid == details.Organization.Oid);

            if (announcement == null)
            {
                return NotFound();
            }

            ViewBag.OrganizationName =
                details.Organization.OrgName;

            ViewBag.FirstName = details.Person.FirstName;
            ViewBag.LastName = details.Person.LastName;

            return View(DetailsView, announcement);
        }
    }
}
