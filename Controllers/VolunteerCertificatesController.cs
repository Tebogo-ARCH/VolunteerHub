using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VolunteerHub.Models;
using VolunteerHub.Models.ViewModels;

namespace VolunteerHub.Controllers
{
    public class VolunteerCertificatesController : Controller
    {
        private readonly VolunteerHubContext _context;

        public VolunteerCertificatesController(VolunteerHubContext context)
        {
            _context = context;
        }


        // =========================================================
        // VIEW VOLUNTEER CERTIFICATES
        // =========================================================

        public async Task<IActionResult> Index()
        {
            string? username =
                HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }


            // -----------------------------------------------------
            // Find logged-in person
            // -----------------------------------------------------

            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.UserName.ToUpper() == username.ToUpper());

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }


            // -----------------------------------------------------
            // Find volunteer
            // -----------------------------------------------------

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v =>
                    v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }


            // -----------------------------------------------------
            // Get certificates belonging ONLY to this volunteer
            // -----------------------------------------------------

            var certificates = await _context.Certificates
                .Where(c => c.Vid == volunteer.Vid)
                .Include(c => c.Project)
                .OrderByDescending(c => c.IssuedDate)
                .Select(c => new VolunteerCertificateViewModel
                {
                    CertId = c.CertId,

                    VolunteerId = c.Vid,

                    ProjectId = c.ProjectId,

                    ProjectName = c.Project.ProjectName,

                    ReportId = c.ReportId,

                    IssuedDate = c.IssuedDate,

                    Description = c.Description ?? string.Empty
                })
                .ToListAsync();


            ViewBag.FirstName = person.FirstName;
            ViewBag.LastName = person.LastName;
            ViewBag.Vid = volunteer.Vid;


            return View(
                "~/Views/Volunteer/Certificates/Index.cshtml",
                certificates
            );
        }


        // =========================================================
        // DOWNLOAD CERTIFICATE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            // -----------------------------------------------------
            // Get logged-in username
            // -----------------------------------------------------

            string? username =
                HttpContext.Session.GetString("Username");

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }


            // -----------------------------------------------------
            // Find logged-in person
            // -----------------------------------------------------

            var person = await _context.Persons
                .FirstOrDefaultAsync(p =>
                    p.UserName.ToUpper() == username.ToUpper());

            if (person == null)
            {
                return RedirectToAction("Login", "Account");
            }


            // -----------------------------------------------------
            // Find volunteer
            // -----------------------------------------------------

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v =>
                    v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }


            // -----------------------------------------------------
            // SECURITY CHECK
            //
            // Only the volunteer who owns the certificate
            // can download it.
            // -----------------------------------------------------

            var certificate = await _context.Certificates
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c =>
                    c.CertId == id &&
                    c.Vid == volunteer.Vid);

            if (certificate == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // Volunteer name
            // -----------------------------------------------------

            string volunteerName =
                $"{person.FirstName} {person.LastName}";


            // -----------------------------------------------------
            // Project name
            // -----------------------------------------------------

            string projectName =
                certificate.Project?.ProjectName
                ?? "Volunteer Project";


            // -----------------------------------------------------
            // Volunteer ID
            // -----------------------------------------------------

            int volunteerId =
                volunteer.Vid;


            // -----------------------------------------------------
            // Issue date
            // -----------------------------------------------------

            string issuedDate =
                certificate.IssuedDate.HasValue
                    ? certificate.IssuedDate.Value
                        .ToString("dd MMMM yyyy")
                    : "Date not specified";


            // -----------------------------------------------------
            // Description
            // -----------------------------------------------------

            string description =
                string.IsNullOrWhiteSpace(
                    certificate.Description)
                    ? "For dedicated volunteer service and valuable contribution."
                    : certificate.Description;


            // =====================================================
            // QUESTPDF LICENSE
            // =====================================================

            QuestPDF.Settings.License =
                LicenseType.Community;


            // =====================================================
            // CREATE CERTIFICATE
            // =====================================================

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // =================================================
                    // PAGE
                    // =================================================

                    page.Size(
                        PageSizes.A4.Landscape()
                    );

                    page.Margin(25);


                    // =================================================
                    // BACKGROUND
                    // =================================================

                    page.Background(
                        "#F7F9FC"
                    );


                    // =================================================
                    // MAIN BORDER
                    // =================================================

                    page.Content()
                        .Border(2)
                        .BorderColor("#AEBECD")
                        .Padding(8)
                        .Border(1)
                        .BorderColor("#AEBECD")
                        .Padding(28)


                        // =================================================
                        // MAIN CONTENT
                        // =================================================

                        .Column(column =>
                        {

                            // =============================================
                            // TOP DECORATIVE BLUE AREA
                            // =============================================

                            column.Item()
                                .AlignCenter()
                                .Width(150)
                                .Height(8)
                                .Background("#2F5DA8");


                            // =============================================
                            // CERTIFICATE
                            // =============================================

                            column.Item()
                                .PaddingTop(10)
                                .AlignCenter()
                                .Text("CERTIFICATE")
                                .FontSize(42)
                                .FontFamily("Times New Roman")
                                .FontColor("#101820");


                            // =============================================
                            // OF ACHIEVEMENT
                            // =============================================

                            column.Item()
                                .PaddingTop(2)
                                .AlignCenter()
                                .Text("O F   A C H I E V E M E N T")
                                .FontSize(21)
                                .Bold()
                                .FontFamily("Times New Roman")
                                .FontColor("#101820");


                            // =============================================
                            // DECORATIVE DIVIDER
                            // =============================================

                            column.Item()
                                .PaddingTop(10)
                                .AlignCenter()
                                .Text("✦")
                                .FontSize(18)
                                .FontColor("#2F5DA8");


                            // =============================================
                            // PRESENTED TO
                            // =============================================

                            column.Item()
                                .PaddingTop(6)
                                .AlignCenter()
                                .Text(
                                    "THIS CERTIFICATE IS PROUDLY PRESENTED TO"
                                )
                                .FontSize(12)
                                .Bold()
                                .LetterSpacing(1.5f)
                                .FontFamily("Times New Roman")
                                .FontColor("#20252B");


                            // =============================================
                            // VOLUNTEER NAME
                            // =============================================

                            column.Item()
                                .PaddingTop(12)
                                .AlignCenter()
                                .Text(volunteerName)
                                .FontSize(31)
                                .Italic()
                                .FontFamily("Times New Roman")
                                .FontColor("#101820");


                            // =============================================
                            // NAME UNDERLINE
                            // =============================================

                            column.Item()
                                .PaddingTop(5)
                                .AlignCenter()
                                .Width(560)
                                .LineHorizontal(1)
                                .LineColor("#AEBECD");


                            // =============================================
                            // DESCRIPTION
                            // =============================================

                            column.Item()
                                .PaddingTop(12)
                                .PaddingHorizontal(35)
                                .AlignCenter()
                                .Text(description)
                                .FontSize(11)
                                .FontFamily("Times New Roman")
                                .FontColor("#30353A")
                                .LineHeight(1.3f);


                            // =============================================
                            // PROJECT
                            // =============================================

                            column.Item()
                                .PaddingTop(10)
                                .AlignCenter()
                                .Text(
                                    "FOR OUTSTANDING VOLUNTEER SERVICE ON"
                                )
                                .FontSize(10)
                                .Bold()
                                .LetterSpacing(1)
                                .FontColor("#666666");


                            column.Item()
                                .PaddingTop(5)
                                .AlignCenter()
                                .Text(projectName)
                                .FontSize(18)
                                .Bold()
                                .FontFamily("Times New Roman")
                                .FontColor("#2F5DA8");


                            // =============================================
                            // VOLUNTEER ID + DATE
                            // =============================================

                            column.Item()
                                .PaddingTop(10)
                                .Row(row =>
                                {

                                    // -----------------------------
                                    // VOLUNTEER ID
                                    // -----------------------------

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Text(
                                            $"Volunteer ID: {volunteerId}"
                                        )
                                        .FontSize(10)
                                        .FontColor("#555555");


                                    // -----------------------------
                                    // DATE
                                    // -----------------------------

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Text(
                                            $"Issued: {issuedDate}"
                                        )
                                        .FontSize(10)
                                        .FontColor("#555555");

                                });


                            // =============================================
                            // SIGNATURES
                            // =============================================

                            column.Item()
                                .PaddingTop(18)
                                .Row(row =>
                                {

                                    // -------------------------------------
                                    // LEFT SIGNATURE
                                    // -------------------------------------

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Column(signature =>
                                        {

                                            signature.Item()
                                                .AlignCenter()
                                                .Text("Signature")
                                                .FontSize(18)
                                                .Italic()
                                                .FontFamily("Times New Roman")
                                                .FontColor("#101820");


                                            signature.Item()
                                                .PaddingTop(2)
                                                .Width(190)
                                                .LineHorizontal(1)
                                                .LineColor("#7A8792");


                                            signature.Item()
                                                .PaddingTop(4)
                                                .AlignCenter()
                                                .Text(
                                                    "VolunteerHub Management"
                                                )
                                                .FontSize(9)
                                                .Bold()
                                                .FontColor("#222222");


                                            signature.Item()
                                                .AlignCenter()
                                                .Text(
                                                    "AUTHORIZED REPRESENTATIVE"
                                                )
                                                .FontSize(8)
                                                .FontColor("#555555");

                                        });


                                    // -------------------------------------
                                    // CENTER SEAL
                                    // -------------------------------------

                                    row.ConstantItem(120)
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Column(seal =>
                                        {

                                            seal.Item()
                                                .AlignCenter()
                                                .Width(75)
                                                .Height(75)
                                                .Background("#D9E4EF")
                                                .Border(3)
                                                .BorderColor("#9EB3C5")
                                                .CornerRadius(40)
                                                .AlignCenter()
                                                .AlignMiddle()
                                                .Text("VH")
                                                .FontSize(24)
                                                .Bold()
                                                .FontColor("#2F5DA8");


                                            seal.Item()
                                                .PaddingTop(4)
                                                .AlignCenter()
                                                .Text(
                                                    "VOLUNTEERHUB"
                                                )
                                                .FontSize(7)
                                                .Bold()
                                                .FontColor("#555555");

                                        });


                                    // -------------------------------------
                                    // RIGHT SIGNATURE
                                    // -------------------------------------

                                    row.RelativeItem()
                                        .AlignCenter()
                                        .Column(signature =>
                                        {

                                            signature.Item()
                                                .AlignCenter()
                                                .Text("Signature")
                                                .FontSize(18)
                                                .Italic()
                                                .FontFamily("Times New Roman")
                                                .FontColor("#101820");


                                            signature.Item()
                                                .PaddingTop(2)
                                                .Width(190)
                                                .LineHorizontal(1)
                                                .LineColor("#7A8792");


                                            signature.Item()
                                                .PaddingTop(4)
                                                .AlignCenter()
                                                .Text(
                                                    "Program Coordinator"
                                                )
                                                .FontSize(9)
                                                .Bold()
                                                .FontColor("#222222");


                                            signature.Item()
                                                .AlignCenter()
                                                .Text(
                                                    "AUTHORIZED REPRESENTATIVE"
                                                )
                                                .FontSize(8)
                                                .FontColor("#555555");

                                        });

                                });

                        });
                });
            });


            // =====================================================
            // GENERATE PDF
            // =====================================================

            byte[] pdfBytes =
                document.GeneratePdf();


            // =====================================================
            // SAFE FILE NAME
            // =====================================================

            string safeProjectName =
                string.Join(
                    "_",
                    projectName.Split(
                        Path.GetInvalidFileNameChars(),
                        StringSplitOptions.RemoveEmptyEntries
                    )
                );


            string fileName =
                $"VolunteerHub_Certificate_Volunteer_{volunteerId}_{safeProjectName}.pdf";


            // =====================================================
            // DOWNLOAD
            // =====================================================

            return File(
                pdfBytes,
                "application/pdf",
                fileName
            );
        }
    }
}