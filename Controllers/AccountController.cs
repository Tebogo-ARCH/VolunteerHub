
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

            _resetTokenProtector = dataProtectionProvider.CreateProtector(
                "VolunteerHub.Account.PasswordReset.v1");
        }


        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var person = _context.Persons
                .FirstOrDefault(p => p.UserName == model.UserName);

            if (person == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid username or password.");

                return View(model);
            }

            if (string.IsNullOrEmpty(person.PasswordHash))
            {
                ModelState.AddModelError(
                    "",
                    "This account does not have a password set.");

                return View(model);
            }

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

            HttpContext.Session.SetString(
                "Username",
                person.UserName);

            HttpContext.Session.SetInt32(
                "Pid",
                person.Pid);

            if (person.MustChangePassword)
            {
                return RedirectToAction(
                    "ChangePassword",
                    "Account",
                    new { id = person.Pid });
            }

            if (_context.Managers.Any(m => m.Pid == person.Pid))
            {
                return RedirectToAction("Index", "Manager");
            }

            if (_context.Staff.Any(s => s.Pid == person.Pid))
            {
                return RedirectToAction("Index", "Staff");
            }

            if (_context.Volunteers.Any(v => v.Pid == person.Pid))
            {
                return RedirectToAction("Index", "Volunteer");
            }

            ModelState.AddModelError(
                "",
                "Your account has not been assigned a role.");

            return View(model);
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ViewBag.Message =
                "If an account matches that email address, a password " +
                "reset can be requested. For local testing, check below " +
                "for a reset link.";

            string email = model.Email.Trim();

            var person = _context.Persons
                .FirstOrDefault(p => p.Email == email);

            if (person != null)
            {
                var tokenPayload = new PasswordResetTokenPayload
                {
                    Pid = person.Pid,
                    Email = person.Email,
                    PasswordHash = person.PasswordHash ?? string.Empty
                };

                string tokenData = JsonSerializer.Serialize(tokenPayload);

                var timeLimitedProtector =
                    _resetTokenProtector.ToTimeLimitedDataProtector();

                string token = timeLimitedProtector.Protect(
                    tokenData,
                    ResetTokenLifetime);

                string? resetLink = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { token },
                    Request.Scheme);

                if (_environment.IsDevelopment() &&
                    !string.IsNullOrWhiteSpace(resetLink))
                {
                    ViewBag.ResetLink = resetLink;
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return View("ResetPasswordInvalid");
            }

            var person = FindPersonFromResetToken(token);

            if (person == null)
            {
                return View("ResetPasswordInvalid");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var person = FindPersonFromResetToken(model.Token);

            if (person == null)
            {
                return View("ResetPasswordInvalid");
            }

            person.PasswordHash = _passwordHasher.HashPassword(
                person,
                model.NewPassword);

            person.MustChangePassword = false;

            _context.SaveChanges();
            TempData["PasswordResetSuccess"] =
                "Your password has been reset successfully. You can now log in.";

            return RedirectToAction("Login", "Account");
        }


        private Person? FindPersonFromResetToken(string token)
        {
            try
            {
                var timeLimitedProtector =
                    _resetTokenProtector.ToTimeLimitedDataProtector();

                string tokenData = timeLimitedProtector.Unprotect(
                    token,
                    out DateTimeOffset expiration);

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

                var person = _context.Persons
                    .FirstOrDefault(p => p.Pid == payload.Pid);

                if (person == null)
                {
                    return null;
                }

                if (!string.Equals(
                    person.Email,
                    payload.Email,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

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
                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        [HttpGet]
        public IActionResult RegisterVolunteer()
        {
            var model = new VolunteerRegistrationViewModel();

            LoadRegistrationLists(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterVolunteer(
            VolunteerRegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadRegistrationLists(model);
                return View(model);
            }

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

            var selectedSkillIds = model.SelectedSkillIds
                .Distinct()
                .ToList();

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

            using var transaction = _context.Database.BeginTransaction();

            try
            {
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

                person.PasswordHash = _passwordHasher.HashPassword(
                    person,
                    model.Password);

                _context.Persons.Add(person);
                _context.SaveChanges();

                var volunteer = new Volunteer
                {
                    Pid = person.Pid,
                    Mid = organization.Mid
                };

                _context.Volunteers.Add(volunteer);
                _context.SaveChanges();

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

                transaction.Commit();

                TempData["RegistrationSuccess"] =
                    "Your volunteer account has been created successfully. You can now log in.";

                return RedirectToAction("Login", "Account");
            }
            catch
            {
                transaction.Rollback();

                ModelState.AddModelError(
                    "",
                    "Registration could not be completed. Please try again.");

                LoadRegistrationLists(model);

                return View(model);
            }
        }


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


        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("Pid");
            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }


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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var person = _context.Persons
                .FirstOrDefault(p => p.Pid == model.Pid);

            if (person == null)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(person.PasswordHash))
            {
                ModelState.AddModelError(
                    "",
                    "This account does not have a password.");

                return View(model);
            }

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

            person.PasswordHash = _passwordHasher.HashPassword(
                person,
                model.NewPassword);

            person.MustChangePassword = false;

            _context.SaveChanges();

            return RedirectToAction("Login", "Account");
        }


        private sealed class PasswordResetTokenPayload
        {
            public int Pid { get; set; }

            public string Email { get; set; } = string.Empty;

            public string PasswordHash { get; set; } = string.Empty;
        }
    }
}
