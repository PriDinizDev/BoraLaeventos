// /Controllers/HomeController.cs
using Bem_vindo.pt.Models;
using Bem_vindo.pt.Services;
using Bem_Vindo.pt.Models;
using Microsoft.AspNetCore.Authorization; // ----- ADICIONADO -----
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace Bem_vindo.pt.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        IEmailSender emailSender)
    {
        _logger = logger;
        _context = context;
        _emailSender = emailSender;
    }

    // GET: /Home/Index ou /
    public IActionResult Index() => View();

    // GET: /Home/Privacy
    public IActionResult Privacy() => View();

    // GET: /Home/Guias?SearchString=palavra
    [HttpGet]
    public async Task<IActionResult> Guias(string searchString)
    {
        ViewData["CurrentFilter"] = searchString;
        var guidesQuery = from g in _context.Guides select g;
        if (!string.IsNullOrEmpty(searchString))
        {
            var searchLower = searchString.ToLower();
            guidesQuery = guidesQuery.Where(g => (g.Titulo != null && g.Titulo.ToLower().Contains(searchLower)) || (g.Entidade != null && g.Entidade.ToLower().Contains(searchLower)) || (g.Processo != null && g.Processo.ToLower().Contains(searchLower)));
        }
        var guides = await guidesQuery.ToListAsync();
        return View(guides);
    }

    // GET: /Home/DetalhesGuia/5
    [HttpGet]
    public async Task<IActionResult> DetalhesGuia(int? id)
    {
        if (id == null) return NotFound();

        var guide = await _context.Guides.FirstOrDefaultAsync(m => m.Id == id);

        if (guide == null) return NotFound();

        // ----- VERIFICAR SE O UTILIZADOR LOGADO JÁ FAVORITOU ESTE GUIA -----
        bool isFavorited = false;
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                // Verifica se existe uma entrada na tabela de junção para este User e Guide
                isFavorited = await _context.UserFavoriteGuides
                                     .AnyAsync(ufg => ufg.UserId == userId && ufg.GuideId == id);
            }
        }
        ViewBag.IsFavorited = isFavorited; // Envia a informação para a View
        // -------------------------------------------------------------------

        return View(guide);
    }

    // ----- NOVA ACTION PARA ADICIONAR FAVORITO -----
    [HttpPost]
    [Authorize] // Só utilizadores logados podem favoritar
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFavorite(int guideId)
    {
        if (guideId == 0) return BadRequest();

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            // Não deve acontecer se [Authorize] funciona, mas é uma salvaguarda
            return Unauthorized();
        }

        // Verificar se o guia existe
        var guideExists = await _context.Guides.AnyAsync(g => g.Id == guideId);
        if (!guideExists) return NotFound("Guia não encontrado.");

        // Verificar se já não está favoritado (evitar duplicados)
        var alreadyFavorited = await _context.UserFavoriteGuides
                                        .AnyAsync(ufg => ufg.UserId == userId && ufg.GuideId == guideId);

        if (!alreadyFavorited)
        {
            var favorite = new UserFavoriteGuide
            {
                UserId = userId,
                GuideId = guideId,
                FavoritedAt = DateTime.UtcNow
            };
            _context.UserFavoriteGuides.Add(favorite);
            await _context.SaveChangesAsync();
            TempData["FavoriteMessage"] = "Guia adicionado aos favoritos!"; // Mensagem opcional
        }

        // Redireciona de volta para a página de detalhes do guia
        return RedirectToAction("DetalhesGuia", new { id = guideId });
    }
    // ===== FIM DA NOVA ACTION =====

    // ===== NOVA ACTION PARA REMOVER FAVORITO =====
    [HttpPost]
    [Authorize] // Só utilizadores logados podem remover
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFavorite(int guideId)
    {
        if (guideId == 0) return BadRequest();

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return Unauthorized();
        }

        // Encontrar a entrada na tabela de junção
        var favoriteEntry = await _context.UserFavoriteGuides
                                    .FirstOrDefaultAsync(ufg => ufg.UserId == userId && ufg.GuideId == guideId);

        if (favoriteEntry != null)
        {
            _context.UserFavoriteGuides.Remove(favoriteEntry);
            await _context.SaveChangesAsync();
            TempData["FavoriteMessage"] = "Guia removido dos favoritos."; // Mensagem opcional
        }

        // Redireciona de volta para a página de detalhes do guia
        return RedirectToAction("DetalhesGuia", new { id = guideId });
    }
    // ===== FIM DA NOVA ACTION =====


    // --- Métodos de Contacto (mantêm-se iguais) ---
    [HttpGet]
    public IActionResult Contact()
    {
        // ... (código existente) ...
        var model = new ContactViewModel();
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            model.Nome = User.FindFirstValue(ClaimTypes.Name) ?? "";
            model.Email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        }
        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        // ... (código existente) ...
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        try
        {
            var adminEmail = "apoio@bem-vindo.pt";
            var subject = $"Novo Contacto do Site: {model.Assunto}";
            var safeMessage = HttpUtility.HtmlEncode(model.Mensagem);
            var messageBody = $@"..."; // Mensagem completa
            await _emailSender.SendEmailAsync(adminEmail, subject, messageBody);
            TempData["SuccessMessage"] = "A sua mensagem foi enviada com sucesso!";
            return RedirectToAction("ContactConfirmation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email de contacto.");
            ModelState.AddModelError("", "Ocorreu um erro inesperado...");
            return View(model);
        }
    }
    [HttpGet]
    public IActionResult ContactConfirmation()
    {
        // ... (código existente) ...
        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        if (ViewBag.SuccessMessage == null) return RedirectToAction("Index");
        return View();
    }

    // --- Métodos de Erro (mantêm-se iguais) ---
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var errorViewModel = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            StatusCode = statusCode
        };
        return View(errorViewModel);
    }
}