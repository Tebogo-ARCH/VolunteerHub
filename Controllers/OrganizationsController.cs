using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolunteerHub.Models;

namespace VolunteerHub.Controllers
{
    public class OrganizationsController : Controller
    {
        private readonly VolunteerHubContext _context;

        public OrganizationsController(VolunteerHubContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var organizations = await _context.Organizations.ToListAsync();

            return View(organizations);
        }
    }
}