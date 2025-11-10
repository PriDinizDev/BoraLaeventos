using Microsoft.AspNetCore.Mvc;

namespace Bem_vindo.pt.Controllers
{
    public class EventosController : Controller
    {
        // /Eventos
        public IActionResult Index() => View();

        // /Eventos/TesteCreate
        public IActionResult TesteCreate() => View();
    }
}
