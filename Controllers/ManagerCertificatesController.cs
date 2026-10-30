
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class ManagerCertificatesController : Controller
    {
        private readonly VolunteerHubContext _context;

        public ManagerCertificatesController(VolunteerHubContext context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int reportId)
        {
            string? username =
                HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.UserName.ToUpper() == username.ToUpper());

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var manager = await _context.Managers
                .FirstOrDefaultAsync(m =>
                    m.Pid == person.Pid);

            if (manager == null)
            {
                return Forbid();
            }

            var report = await _context.Reports
                .Include(r => r.Project)
                .Include(r => r.VidNavigation)
                    .ThenInclude(v => v.PidNavigation)
                .FirstOrDefaultAsync(r =>
                    r.ReportId == reportId &&
                    r.Mid == manager.Mid);

            if (report == null)
            {
                return NotFound();
            }

            if (!string.Equals(
                    report.Status,
                    "GENERATED",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "A certificate can only be issued for a GENERATED report.";

                return RedirectToAction(
                    "Details",
                    "ManagerReport",
                    new { id = reportId });
            }

            if (!string.Equals(
                    report.Project?.Status,
                    "COMPLETED",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] =
                    "A certificate cannot be issued while the project is not COMPLETED.";

                return RedirectToAction(
                    "Details",
                    "ManagerReport",
                    new { id = reportId });
            }

            var existingCertificate = await _context.Certificates
                .FirstOrDefaultAsync(c =>
                    c.ReportId == report.ReportId);

            if (existingCertificate != null)
            {
                TempData["ErrorMessage"] =
                    "A certificate has already been issued for this report.";

                return RedirectToAction(
                    "Details",
                    "ManagerReport",
                    new { id = reportId });
            }

            string volunteerName =
                $"{report.VidNavigation.PidNavigation.FirstName} " +
                $"{report.VidNavigation.PidNavigation.LastName}";

            var certificate = new Certificate
            {
                Vid = report.Vid,

                ProjectId = report.ProjectId,

                ReportId = report.ReportId,

                IssuedDate =
                    DateOnly.FromDateTime(DateTime.Today),

                Description =
                    $"This certificate is awarded to {volunteerName} " +
                    $"for successfully completing volunteer service " +
                    $"on the project \"{report.Project.ProjectName}\"."
            };

            _context.Certificates.Add(certificate);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Certificate issued successfully.";

            return RedirectToAction(
                "Details",
                "ManagerReport",
                new { id = reportId });
        }
    }
}
