using System.Threading.Tasks;
using System.Web.Mvc;

namespace Hangfire.Harness.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}