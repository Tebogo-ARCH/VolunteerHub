using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class VolunteerOrganizationController : Controller
    {
        private readonly VolunteerHubContext _context;

        public VolunteerOrganizationController(VolunteerHubContext context)
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

            var volunteer = _context.Volunteers
                .FirstOrDefault(v => v.Pid == person.Pid);

            if (volunteer == null)
            {
                return Forbid();
            }

            var organization = _context.Organizations
                .Include(o => o.Address)
                .FirstOrDefault(o => o.Mid == volunteer.Mid);

            if (organization == null)
            {
                return NotFound("Organization not found.");
            }

            return View(
                "~/Views/Volunteer/Organization/Index.cshtml",
                organization
            );
        }
    }
}