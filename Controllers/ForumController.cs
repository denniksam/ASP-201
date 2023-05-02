using ASP_201.Data;
using ASP_201.Models.Forum;
using Microsoft.AspNetCore.Mvc;

namespace ASP_201.Controllers
{
    public class ForumController : Controller
    {
        private readonly DataContext _dataContext;

        public ForumController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public IActionResult Index()
        {
            ForumIndexModel model = new()
            {
                UserCanCreate = HttpContext.User.Identity?.IsAuthenticated == true,
                Sections = _dataContext.Sections.Where(s => s.DeletedDt == null).ToList()
            };
            return View(model);
        }

        [HttpPost]
        public RedirectToActionResult CreateSection()
        {
            return RedirectToAction(nameof(Index));
        }
    }
}
/* Згадати про механізм Redirect
 * Описати модель прийому даних від форми (створення нового розділу)
 * Впровадити цю модель у метод CreateSection() контроллера
 * (вивести дані логером)
 * Додати до форми кнопку надсилання та випробувати передачу даних
 */
