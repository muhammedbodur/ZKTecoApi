using System.Web.Mvc;

namespace ZKTecoApi.Controllers
{
    /// <summary>
    /// Ana sayfa controller - Swagger dokümantasyonuna yönlendirir
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Ana sayfa - otomatik olarak Swagger UI'a yönlendirir
        /// </summary>
        /// <returns>Swagger UI'a redirect</returns>
        public ActionResult Index()
        {
            return Redirect("~/swagger");
        }
    }
}
