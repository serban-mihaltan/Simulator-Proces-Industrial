using Microsoft.AspNetCore.Mvc;
using MonitorApi.Data;
using System.Linq;

namespace MonitorApi.Controllers
{
    public class HomeController : Controller
    {
        private readonly MonitorDbContext _db;
        public HomeController(MonitorDbContext db) => _db = db;
        public IActionResult Index()
        {
            var model = _db.LogRecords.OrderByDescending(r => r.Id).ToList();
            return View(model);
        }
    }
}