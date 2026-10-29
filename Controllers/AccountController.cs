
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.Json;
using VolunteerHub.Models;
using VolunteerHub.Models.ViewModels;

namespace VolunteerHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly VolunteerHubContext _context;
        private readonly PasswordHasher<Person> _passwordHasher;
        private readonly IDataProtector _resetTokenProtector;
        private readonly IWebHostEnvironment _environment;

        // Password reset links expire after 30 minutes.
        private static readonly TimeSpan ResetTokenLifetime =
            TimeSpan.FromMinutes(30);

        public AccountController(
            VolunteerHubContext context,
            IDataProtectionProvider dataProtectionProvider,
            IWebHostEnvironment environment)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Person>();
            _environment = environment;

            // Creates a dedicated protector for password-reset tokens.
            _resetTokenProtector = dataProtectionProvider.CreateProtector(
                "VolunteerHub.Account.PasswordReset.v1");
        }

        // =========================================================
        // LOGIN - GET
        // Displays the existing Login page.
        // =========================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // =========================================================
        // LOGIN - POST
        // Checks the username and password and directs the user
        // to the correct dashboard based on their role.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find the person using their username.
            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == model.UserName);

            if (person == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid username or password.");

                return View(model);
            }

            // Make sure the account has a stored password hash.
            if (string.IsNullOrEmpty(person.PasswordHash))
            {
                ModelState.AddModelError(
                    "",
                    "This account does not have a password set.");

                return View(model);
            }

            // Compare the entered password with the stored password hash.
            var result = _passwordHasher.VerifyHashedPassword(
                person,
                person.PasswordHash,
                model.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid username or password.");

                return View(model);
            }

            // Save the logged-in user's details in the session.
            HttpContext.Session.SetString(
                "Username",
                person.UserName);

            HttpContext.Session.SetInt32(
                "Pid",
                person.Pid);

            // Require users with an initial password to change it.
            if (person.MustChangePassword)
            {
                return RedirectToAction(
                    "ChangePassword",
                    "Account",
                    new { id = person.Pid });
            }

            // Direct managers to the Manager dashboard.
            if (_context.Managers.Any(m => m.Pid == person.Pid))
            {
                return RedirectToAction("Index", "Manager");
            }

            // Direct staff to the Staff dashboard.
            if (_context.Staff.Any(s => s.Pid == person.Pid))
            {
                return RedirectToAction("Index", "Staff");
            }

            // Direct volunteers to the Volunteer dashboard.
            if (_context.Volunteers.Any(v => v.Pid == person.Pid))
            {
                return RedirectToAction("Index", "Volunteer");
            }

            // If no role is found, display an error.
            ModelState.AddModelError(
                "",
                "Your account has not been assigned a role.");

            return View(model);
        }

        // =========================================================
        // FORGOT PASSWORD - GET
        // Displays the page where users enter their email address.
        // =========================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // =========================================================
        // FORGOT PASSWORD - POST
        // Creates a protected reset link for local testing.
        // No email is sent by this development-only implementation.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Use the same general message whether or not the email
            // belongs to an account, to avoid revealing registered users.
            ViewBag.Message =
                "If an account matches that email address, a password " +
                "reset can be requested. For local testing, check below " +
                "for a reset link.";

            string email = model.Email.Trim();

            // Find the account associated with the submitted email.
            var person = _context.Persons
                .FirstOrDefault(p => p.Email == email);

            // Generate a reset token only if an account was found.
            if (person != null)
            {
                // Store the person's ID, email and current password hash
                // inside the protected token.
                var tokenPayload = new PasswordResetTokenPayload
                {
                    Pid = person.Pid,
                    Email = person.Email,
                    PasswordHash = person.PasswordHash ?? string.Empty
                };

                string tokenData = JsonSerializer.Serialize(tokenPayload);

                // Protect the token and give it a 30-minute lifetime.
                var timeLimitedProtector =
                    _resetTokenProtector.ToTimeLimitedDataProtector();

                string token = timeLimitedProtector.Protect(
                    tokenData,
                    ResetTokenLifetime);

                // Create the link that opens the reset-password page.
                string? resetLink = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { token },
                    Request.Scheme);

                // Show the link only while running in Development.
                if (_environment.IsDevelopment() &&
                    !string.IsNullOrWhiteSpace(resetLink))
                {
                    ViewBag.ResetLink = resetLink;
                }
            }

            return View(model);
        }

        // =========================================================
        // RESET PASSWORD - GET
        // Checks the link before displaying the new-password form.
        // =========================================================

        [HttpGet]
        public IActionResult ResetPassword(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return View("ResetPasswordInvalid");
            }

            // Confirm that the token is valid and has not expired.
            var person = FindPersonFromResetToken(token);

            if (person == null)
            {
                return View("ResetPasswordInvalid");
            }

            // Pass the token to the form so it can be checked again
            // when the user submits their new password.
            var model = new ResetPasswordViewModel
            {
                Token = token
            };

            return View(model);
        }

        // =========================================================
        // RESET PASSWORD - POST
        // Checks the token and saves the user's new password hash.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate the token again before changing the password.
            var person = FindPersonFromResetToken(model.Token);

            if (person == null)
            {
                return View("ResetPasswordInvalid");
            }

            // Hash the new password using the same hasher as Login.
            person.PasswordHash = _passwordHasher.HashPassword(
                person,
                model.NewPassword);

            // The user no longer needs to change an initial password.
            person.MustChangePassword = false;

            // Save the new password hash in the database.
            _context.SaveChanges();

            // Display a success message after redirecting to Login.
            TempData["PasswordResetSuccess"] =
                "Your password has been reset successfully. You can now log in.";

            return RedirectToAction("Login", "Account");
        }

        // =========================================================
        // HELPER: FIND PERSON FROM RESET TOKEN
        // Rejects invalid, expired or previously used reset links.
        // =========================================================

        private Person? FindPersonFromResetToken(string token)
        {
            try
            {
                // Unprotect the token and check its expiration time.
                var timeLimitedProtector =
                    _resetTokenProtector.ToTimeLimitedDataProtector();

                string tokenData = timeLimitedProtector.Unprotect(
                    token,
                    out DateTimeOffset expiration);

                // Convert the protected token data back into an object.
                var payload =
                    JsonSerializer.Deserialize<PasswordResetTokenPayload>(
                        tokenData);

                if (payload == null)
                {
                    return null;
                }

                if (payload.Pid <= 0)
                {
                    return null;
                }

                if (expiration <= DateTimeOffset.UtcNow)
                {
                    return null;
                }

                // Find the account using the ID stored in the token.
                var person = _context.Persons
                    .FirstOrDefault(p => p.Pid == payload.Pid);

                if (person == null)
                {
                    return null;
                }

                // Ensure the account email has not changed since
                // the reset token was generated.
                if (!string.Equals(
                    person.Email,
                    payload.Email,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                // Ensure the password has not changed since the
                // token was generated. This prevents reusing an old link.
                if (!string.Equals(
                    person.PasswordHash ?? string.Empty,
                    payload.PasswordHash,
                    StringComparison.Ordinal))
                {
                    return null;
                }

                return person;
            }
            catch (CryptographicException)
            {
                // Invalid or expired protected tokens are rejected.
                return null;
            }
            catch (JsonException)
            {
                // Malformed token data is rejected.
                return null;
            }
        }

        // =========================================================
        // VOLUNTEER REGISTRATION - GET
        // Displays the registration form and loads its dropdowns.
        // =========================================================

        [HttpGet]
        public IActionResult RegisterVolunteer()
        {
            var model = new VolunteerRegistrationViewModel();

            LoadRegistrationLists(model);

            return View(model);
        }

        // =========================================================
        // VOLUNTEER REGISTRATION - POST
        // Validates the submitted details and creates a volunteer.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterVolunteer(
            VolunteerRegistrationViewModel model)
        {
            // Reload dropdowns if validation fails.
            if (!ModelState.IsValid)
            {
                LoadRegistrationLists(model);
                return View(model);
            }

            // Check that the username is not already in use.
            var usernameExists = _context.Persons
                .Any(p => p.UserName == model.UserName);

            if (usernameExists)
            {
                ModelState.AddModelError(
                    "UserName",
                    "This username is already in use.");

                LoadRegistrationLists(model);
                return View(model);
            }

            // Check that the email address is not already registered.
            var emailExists = _context.Persons
                .Any(p => p.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email address is already registered.");

                LoadRegistrationLists(model);
                return View(model);
            }

            // Find the organization selected by the volunteer.
            var organization = _context.Organizations
                .FirstOrDefault(o => o.Oid == model.OrganizationId);

            if (organization == null)
            {
                ModelState.AddModelError(
                    "OrganizationId",
                    "Please select a valid organization.");

                LoadRegistrationLists(model);
                return View(model);
            }

            // Check that the selected organization has a manager.
            var managerExists = _context.Managers
                .Any(m => m.Mid == organization.Mid);

            if (!managerExists)
            {
                ModelState.AddModelError(
                    "OrganizationId",
                    "The selected organization does not currently have a manager.");

                LoadRegistrationLists(model);
                return View(model);
            }

            // Remove duplicate selected skill IDs.
            var selectedSkillIds = model.SelectedSkillIds
                .Distinct()
                .ToList();

            // Check that all selected skills exist.
            if (selectedSkillIds.Any())
            {
                var validSkillCount = _context.Skills
                    .Count(s => selectedSkillIds.Contains(s.SkillId));

                if (validSkillCount != selectedSkillIds.Count)
                {
                    ModelState.AddModelError(
                        "SelectedSkillIds",
                        "One or more selected skills are invalid.");

                    LoadRegistrationLists(model);
                    return View(model);
                }
            }

            // Use a transaction so registration succeeds completely
            // or rolls back if a database operation fails.
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // Create and save the volunteer's address.
                var address = new Address
                {
                    StreetAddress = model.StreetAddress,
                    Suburb = model.Suburb,
                    City = model.City,
                    Province = model.Province,
                    PostalCode = model.PostalCode
                };

                _context.Addresses.Add(address);
                _context.SaveChanges();

                // Create the person's account information.
                var person = new Person
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    UserName = model.UserName,
                    Email = model.Email,
                    Phone = model.Phone,
                    AddressId = address.AddressId,
                    JoinedAt = DateTime.Now,
                    Status = "ACTIVE",
                    MustChangePassword = false
                };

                // Store a password hash, not the original password.
                person.PasswordHash = _passwordHasher.HashPassword(
                    person,
                    model.Password);

                _context.Persons.Add(person);
                _context.SaveChanges();

                // Create the volunteer record and link it to the
                // manager of the selected organization.
                var volunteer = new Volunteer
                {
                    Pid = person.Pid,
                    Mid = organization.Mid
                };

                _context.Volunteers.Add(volunteer);
                _context.SaveChanges();

                // Save each selected skill for the new volunteer.
                foreach (var skillId in selectedSkillIds)
                {
                    var volunteerSkill = new VolunteerSkill
                    {
                        Vid = volunteer.Vid,
                        SkillId = skillId
                    };

                    _context.VolunteerSkills.Add(volunteerSkill);
                }

                _context.SaveChanges();

                // Commit the address, person, volunteer and skills.
                transaction.Commit();

                TempData["RegistrationSuccess"] =
                    "Your volunteer account has been created successfully. You can now log in.";

                return RedirectToAction("Login", "Account");
            }
            catch
            {
                // Undo database changes if registration fails.
                transaction.Rollback();

                ModelState.AddModelError(
                    "",
                    "Registration could not be completed. Please try again.");

                LoadRegistrationLists(model);

                return View(model);
            }
        }

        // =========================================================
        // LOAD ORGANIZATIONS AND SKILLS
        // Fills the dropdown lists used by volunteer registration.
        // =========================================================

        private void LoadRegistrationLists(
            VolunteerRegistrationViewModel model)
        {
            model.Organizations = _context.Organizations
                .OrderBy(o => o.OrgName)
                .Select(o => new SelectListItem
                {
                    Value = o.Oid.ToString(),
                    Text = o.OrgName
                })
                .ToList();

            model.Skills = _context.Skills
                .OrderBy(s => s.SkillName)
                .Select(s => new SelectListItem
                {
                    Value = s.SkillId.ToString(),
                    Text = s.SkillName
                })
                .ToList();
        }

        // =========================================================
        // LOGOUT
        // Clears the current session and returns to Login.
        // =========================================================

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("Pid");
            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }

        // =========================================================
        // CHANGE PASSWORD - GET
        // Displays the change-password form for the specified user.
        // =========================================================

        [HttpGet]
        public IActionResult ChangePassword(int id)
        {
            var person = _context.Persons
                .FirstOrDefault(p => p.Pid == id);

            if (person == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordViewModel
            {
                Pid = person.Pid
            };

            ViewBag.UserName = person.UserName;

            return View(model);
        }

        // =========================================================
        // CHANGE PASSWORD - POST
        // Verifies the current password before saving a new one.
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find the account being updated.
            var person = _context.Persons
                .FirstOrDefault(p => p.Pid == model.Pid);

            if (person == null)
            {
                return NotFound();
            }

            // Ensure the account has a stored password hash.
            if (string.IsNullOrEmpty(person.PasswordHash))
            {
                ModelState.AddModelError(
                    "",
                    "This account does not have a password.");

                return View(model);
            }

            // Check the current password before allowing a change.
            var result = _passwordHasher.VerifyHashedPassword(
                person,
                person.PasswordHash,
                model.CurrentPassword);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "CurrentPassword",
                    "The current password is incorrect.");

                return View(model);
            }

            // Hash and save the new password.
            person.PasswordHash = _passwordHasher.HashPassword(
                person,
                model.NewPassword);

            // Clear the initial-password-change requirement.
            person.MustChangePassword = false;

            _context.SaveChanges();

            return RedirectToAction("Login", "Account");
        }

        // =========================================================
        // PRIVATE RESET TOKEN DATA
        // Contains the account information used to validate a link.
        // =========================================================

        private sealed class PasswordResetTokenPayload
        {
            public int Pid { get; set; }

            public string Email { get; set; } = string.Empty;

            public string PasswordHash { get; set; } = string.Empty;
        }
    }
}
