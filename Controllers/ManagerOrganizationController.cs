
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;
using VolunteerHub.Models.ViewModels;

namespace VolunteerHub.Controllers
{
    public class ManagerOrganizationController : Controller
    {
        private readonly VolunteerHubContext _context;

        public ManagerOrganizationController(VolunteerHubContext context)
        {
            _context = context;
        }

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

            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            var organization = _context.Organizations
                .Include(o => o.Address)
                .FirstOrDefault(o => o.Mid == manager.Mid);

            if (organization == null)
            {
                return NotFound();
            }

            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            return View(
                "~/Views/Manager/Organization/Index.cshtml",
                organization
            );
        }

        [HttpGet]
        public IActionResult Edit()
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

            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            var organization = _context.Organizations
                .Include(o => o.Address)
                .FirstOrDefault(o => o.Mid == manager.Mid);

            if (organization == null)
            {
                return NotFound();
            }

            var model = new ManagerOrganizationEditViewModel
            {
                Oid = organization.Oid,

                OrgName = organization.OrgName,
                Description = organization.Description,
                Email = organization.Email,
                Phone = organization.Phone,

                AddressId = organization.AddressId ?? 0,

                StreetAddress = organization.Address?.StreetAddress ?? "",
                Suburb = organization.Address?.Suburb,
                City = organization.Address?.City ?? "",
                Province = organization.Address?.Province ?? "",
                PostalCode = organization.Address?.PostalCode,

                Mid = manager.Mid,
                ManagerFirstName = person.FirstName,
                ManagerLastName = person.LastName
            };

            return View(
                "~/Views/Manager/Organization/Edit.cshtml",
                model
            );
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            ManagerOrganizationEditViewModel model)
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

            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            var organization = _context.Organizations
                .Include(o => o.Address)
                .FirstOrDefault(o => o.Mid == manager.Mid);

            if (organization == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.Oid = organization.Oid;
                model.AddressId = organization.AddressId ?? 0;
                model.Mid = manager.Mid;

                model.ManagerFirstName = person.FirstName;
                model.ManagerLastName = person.LastName;

                return View(
                    "~/Views/Manager/Organization/Edit.cshtml",
                    model
                );
            }

            organization.OrgName = model.OrgName;
            organization.Description = model.Description;
            organization.Email = model.Email;
            organization.Phone = model.Phone;


            if (organization.Address != null)
            {
                organization.Address.StreetAddress =
                    model.StreetAddress;

                organization.Address.Suburb =
                    model.Suburb;

                organization.Address.City =
                    model.City;

                organization.Address.Province =
                    model.Province;

                organization.Address.PostalCode =
                    model.PostalCode;
            }


            _context.SaveChanges();


            return RedirectToAction("Index");
        }
    }
}
