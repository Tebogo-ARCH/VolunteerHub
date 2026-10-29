
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    // Handles the creation of certificates for completed volunteer projects.
    public class ManagerCertificatesController : Controller
    {
        // Gives this controller access to the VolunteerHub database.
        private readonly VolunteerHubContext _context;

        // Constructor: receives the database context through dependency injection.
        public ManagerCertificatesController(VolunteerHubContext context)
        {
            _context = context;
        }

        // Handles the POST request to generate a certificate.
        // The report ID identifies the report for which the certificate is issued.
        [HttpPost]
        // Checks that the submitted form contains a valid anti-forgery token.
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int reportId)
        {
            // ---------------------------------------------------------
            // 1. GET LOGGED-IN USERNAME
            // ---------------------------------------------------------

            // Retrieve the username saved in the session during login.
            string? username =
                HttpContext.Session.GetString("Username");

            // If the session does not contain a username,
            // send the user to the Login page.
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // ---------------------------------------------------------
            // 2. FIND LOGGED-IN PERSON
            // ---------------------------------------------------------

            // Search for the person whose username matches the session.
            // FirstOrDefaultAsync performs the database query asynchronously.
            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.UserName.ToUpper() == username.ToUpper());

            // If no person matches the username, return to Login.
            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // ---------------------------------------------------------
            // 3. FIND MANAGER
            // ---------------------------------------------------------

            // Find the manager record linked to the person's PID.
            var manager = await _context.Managers
                .FirstOrDefaultAsync(m =>
                    m.Pid == person.Pid);

            // If the logged-in person is not a manager, deny access.
            if (manager == null)
            {
                return Forbid();
            }

            // ---------------------------------------------------------
            // 4. FIND THE REPORT
            // ---------------------------------------------------------

            // Retrieve the requested report and load its related project
            // and volunteer/person information.
            //
            // The report must belong to the logged-in manager.
            // This prevents a manager from issuing certificates
            // for another manager's reports.
            var report = await _context.Reports
                .Include(r => r.Project)
                .Include(r => r.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .FirstOrDefaultAsync(r =>
                    r.ReportId == reportId &&
                    r.Mid == manager.Mid);

            // If the report does not exist or does not belong to
            // this manager, return a 404 Not Found response.
            if (report == null)
            {
                return NotFound();
            }

            // ---------------------------------------------------------
            // 5. CHECK REPORT STATUS
            // ---------------------------------------------------------

            // A certificate can only be issued when the report status
            // is GENERATED. Ignore differences in letter casing.
            if (!string.Equals(
                    report.Status,
                    "GENERATED",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Store an error message temporarily so the next page
                // can display it after the redirect.
                TempData["ErrorMessage"] =
                    "A certificate can only be issued for a GENERATED report.";

                // Return to the report details page.
                return RedirectToAction(
                    "Details",
                    "ManagerReport",
                    new { id = reportId });
            }

            // ---------------------------------------------------------
            // 6. CHECK PROJECT STATUS
            // ---------------------------------------------------------

            // The project must be COMPLETED before a certificate
            // can be issued. The ?. operator safely checks the
            // project status if the Project navigation property is null.
            if (!string.Equals(
                    report.Project?.Status,
                    "COMPLETED",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Tell the user why the certificate cannot be issued.
                TempData["ErrorMessage"] =
                    "A certificate cannot be issued while the project is not COMPLETED.";

                return RedirectToAction(
                    "Details",
                    "ManagerReport",
                    new { id = reportId });
            }

            // ---------------------------------------------------------
            // 7. CHECK FOR AN EXISTING CERTIFICATE
            // ---------------------------------------------------------

            // Search for a certificate already linked to this report.
            // This helps prevent duplicate certificates for one report.
            var existingCertificate = await _context.Certificates
                .FirstOrDefaultAsync(c =>
                    c.ReportId == report.ReportId);

            // If a certificate already exists, do not create another.
            if (existingCertificate != null)
            {
                TempData["ErrorMessage"] =
                    "A certificate has already been issued for this report.";

                return RedirectToAction(
                    "Details",
                    "ManagerReport",
                    new { id = reportId });
            }

            // ---------------------------------------------------------
            // 8. GET VOLUNTEER NAME
            // ---------------------------------------------------------

            // Combine the volunteer's first and last names into one string.
            string volunteerName =
                $"{report.VidNavigation.PidNavigation.FirstName} " +
                $"{report.VidNavigation.PidNavigation.LastName}";

            // ---------------------------------------------------------
            // 9. CREATE CERTIFICATE OBJECT
            // ---------------------------------------------------------

            // Create a new certificate and populate its properties
            // using information from the report and related records.
            var certificate = new Certificate
            {
                // Identify the volunteer receiving the certificate.
                Vid = report.Vid,

                // Link the certificate to the completed project.
                ProjectId = report.ProjectId,

                // Link the certificate to the report.
                ReportId = report.ReportId,

                // Store today's date as the certificate issue date.
                IssuedDate =
                    DateOnly.FromDateTime(DateTime.Today),

                // Create a description containing the volunteer's name
                // and the name of the completed project.
                Description =
                    $"This certificate is awarded to {volunteerName} " +
                    $"for successfully completing volunteer service " +
                    $"on the project \"{report.Project.ProjectName}\"."
            };

            // ---------------------------------------------------------
            // 10. SAVE CERTIFICATE
            // ---------------------------------------------------------

            // Mark the new certificate for insertion into the database.
            _context.Certificates.Add(certificate);

            // Save the new certificate asynchronously.
            await _context.SaveChangesAsync();

            // ---------------------------------------------------------
            // 11. DISPLAY SUCCESS MESSAGE
            // ---------------------------------------------------------

            // TempData keeps this message available for the next request.
            TempData["SuccessMessage"] =
                "Certificate issued successfully.";

            // ---------------------------------------------------------
            // 12. RETURN TO REPORT DETAILS
            // ---------------------------------------------------------

            // Redirect the manager to the details of the report
            // for which the certificate was issued.
            return RedirectToAction(
                "Details",
                "ManagerReport",
                new { id = reportId });
        }
    }
}
