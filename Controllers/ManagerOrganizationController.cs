
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;
using VolunteerHub.Models.ViewModels;

namespace VolunteerHub.Controllers
{
    // Handles viewing and editing the manager's organization.
    public class ManagerOrganizationController : Controller
    {
        // Provides access to the database.
        private readonly VolunteerHubContext _context;

        // Constructor: stores the injected database context.
        public ManagerOrganizationController(VolunteerHubContext context)
        {
            _context = context;
        }

        // ==========================================
        // VIEW ORGANIZATION
        // ==========================================

        // Displays the organization belonging to the logged-in manager.
        public IActionResult Index()
        {
            // Get the username saved during login.
            var username = HttpContext.Session.GetString("Username");

            // If no username exists in the session, redirect to Login.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the person's record using the session username.
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            // If the person cannot be found, return to Login.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the manager record linked to this person.
            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            // If the person is not a manager, return to the dashboard.
            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            // Find the organization managed by this manager.
            // Include() also loads the organization's related address.
            var organization = _context.Organizations
                .Include(o => o.Address)
                .FirstOrDefault(o => o.Mid == manager.Mid);

            // If the organization does not exist, return Not Found.
            if (organization == null)
            {
                return NotFound();
            }

            // Pass manager information to the View for display.
            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.UserName = person.UserName;
            ViewBag.Mid = manager.Mid;

            // Return the organization page with the organization
            // object as the View's model.
            return View(
                "~/Views/Manager/Organization/Index.cshtml",
                organization
            );
        }


        // ==========================================
        // EDIT ORGANIZATION - GET
        // ==========================================

        // Displays the form used to edit organization information.
        [HttpGet]
        public IActionResult Edit()
        {
            // Retrieve the logged-in username from the session.
            var username = HttpContext.Session.GetString("Username");

            // Redirect to Login if the user is not logged in.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the logged-in person's record.
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            // Return to Login if the person cannot be found.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the manager account connected to this person.
            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            // Return to the manager dashboard if no manager record exists.
            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            // Retrieve the manager's organization and its address.
            var organization = _context.Organizations
                .Include(o => o.Address)
                .FirstOrDefault(o => o.Mid == manager.Mid);

            // Stop if the organization cannot be found.
            if (organization == null)
            {
                return NotFound();
            }

            // Copy the organization's existing values into a ViewModel.
            // The ViewModel supplies the data needed by the edit form.
            var model = new ManagerOrganizationEditViewModel
            {
                Oid = organization.Oid,

                // Organization details.
                OrgName = organization.OrgName,
                Description = organization.Description,
                Email = organization.Email,
                Phone = organization.Phone,

                // Use 0 if the organization has no AddressId.
                AddressId = organization.AddressId ?? 0,

                // Copy address details into the form.
                // ?? supplies a fallback value when a value is null.
                StreetAddress = organization.Address?.StreetAddress ?? "",
                Suburb = organization.Address?.Suburb,
                City = organization.Address?.City ?? "",
                Province = organization.Address?.Province ?? "",
                PostalCode = organization.Address?.PostalCode,

                // Manager identification and display information.
                Mid = manager.Mid,
                ManagerFirstName = person.FirstName,
                ManagerLastName = person.LastName
            };

            // Display the edit form with the populated ViewModel.
            return View(
                "~/Views/Manager/Organization/Edit.cshtml",
                model
            );
        }


        // ==========================================
        // EDIT ORGANIZATION - POST
        // ==========================================

        // Receives and processes the submitted organization edit form.
        [HttpPost]
        // Validates the anti-forgery token to help protect the form.
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            ManagerOrganizationEditViewModel model)
        {
            // Retrieve the username saved during login.
            var username = HttpContext.Session.GetString("Username");

            // Require the user to be logged in.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the logged-in person's record.
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == username);

            // Redirect to Login if the person cannot be found.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the manager record linked to the person.
            var manager = _context.Managers
                .FirstOrDefault(m => m.Pid == person.Pid);

            // If no manager record exists, return to the dashboard.
            if (manager == null)
            {
                return RedirectToAction("Index", "Manager");
            }

            // ==========================================
            // FIND ORGANIZATION BELONGING TO MANAGER
            // ==========================================

            // Retrieve only the organization associated with this manager.
            // Load the related address so it can also be updated.
            var organization = _context.Organizations
                .Include(o => o.Address)
                .FirstOrDefault(o => o.Mid == manager.Mid);

            // Stop if the organization cannot be found.
            if (organization == null)
            {
                return NotFound();
            }

            // ==========================================
            // VALIDATION
            // ==========================================

            // Check whether the submitted form passed validation.
            // ModelState contains validation results for the submitted values.
            if (!ModelState.IsValid)
            {
                // Restore values required by the edit page before
                // displaying the form again with validation errors.
                model.Oid = organization.Oid;
                model.AddressId = organization.AddressId ?? 0;
                model.Mid = manager.Mid;

                model.ManagerFirstName = person.FirstName;
                model.ManagerLastName = person.LastName;

                // Return the same form so the user can correct errors.
                return View(
                    "~/Views/Manager/Organization/Edit.cshtml",
                    model
                );
            }

            // ==========================================
            // UPDATE ORGANIZATION
            // ==========================================

            // Copy the submitted organization details onto the
            // existing organization object tracked by Entity Framework.
            organization.OrgName = model.OrgName;
            organization.Description = model.Description;
            organization.Email = model.Email;
            organization.Phone = model.Phone;


            // ==========================================
            // UPDATE ADDRESS
            // ==========================================

            // Only update address fields if the organization
            // already has an associated Address record.
            if (organization.Address != null)
            {
                // Copy the submitted address fields to the existing address.
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


            // ==========================================
            // SAVE CHANGES
            // ==========================================

            // Save the organization and address changes to the database.
            _context.SaveChanges();


            // ==========================================
            // RETURN TO ORGANIZATION PAGE
            // ==========================================

            // Redirect to the Index action after a successful update.
            return RedirectToAction("Index");
        }
    }
}
