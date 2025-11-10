using Bem_vindo.pt.Models;
using Bem_Vindo.pt.Models;
using Microsoft.AspNetCore.Authorization; // Necessário para [Authorize]
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Bem_vindo.pt.Controllers;

// ===== AUTORIZAÇÃO ATIVADA =====
// Apenas utilizadores com o Role "Administrador" podem aceder a qualquer Action neste Controller
[Authorize(Roles = "Administrador")]
public class GuidesController : Controller
{
    private readonly ApplicationDbContext _context;

    public GuidesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Guides
    public async Task<IActionResult> Index()
    {
        // Retorna a View Index com a lista de todos os guias
        return View(await _context.Guides.ToListAsync());
    }

    // GET: /Guides/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var guide = await _context.Guides
            .FirstOrDefaultAsync(m => m.Id == id);
        if (guide == null)
        {
            return NotFound();
        }

        return View(guide); // Retorna a View Details com os dados do guia encontrado
    }

    // GET: /Guides/Create
    [HttpGet]
    public IActionResult Create()
    {
        // Apenas mostra a View Create (formulário vazio)
        return View();
    }

    // POST: /Guides/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guide guide)
    {
        if (ModelState.IsValid)
        {
            _context.Add(guide);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index)); // Volta para a lista após sucesso
        }
        return View(guide); // Volta para o formulário se houver erro de validação
    }

    // GET: /Guides/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var guide = await _context.Guides.FindAsync(id);
        if (guide == null)
        {
            return NotFound();
        }
        return View(guide); // Retorna a View Edit com o formulário preenchido
    }

    // POST: /Guides/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Guide guide)
    {
        if (id != guide.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(guide);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GuideExists(guide.Id))
                {
                    return NotFound();
                }
                else
                {
                    ModelState.AddModelError("", "Não foi possível guardar. O registo foi modificado por outro utilizador.");
                    return View(guide);
                }
            }
            return RedirectToAction(nameof(Index)); // Volta para a lista após sucesso
        }
        return View(guide); // Volta para o formulário se houver erro de validação
    }

    // GET: /Guides/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var guide = await _context.Guides
            .FirstOrDefaultAsync(m => m.Id == id);
        if (guide == null)
        {
            return NotFound();
        }

        return View(guide); // Retorna a View Delete para confirmação
    }

    // POST: /Guides/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var guide = await _context.Guides.FindAsync(id);
        if (guide != null)
        {
            _context.Guides.Remove(guide);
            await _context.SaveChangesAsync();
        }
        // else { TempData["ErrorMessage"] = "Erro: O guia já não existe."; } // Opcional

        return RedirectToAction(nameof(Index)); // Volta sempre para a lista
    }


    // Método auxiliar privado
    private bool GuideExists(int id)
    {
        // Certifique-se que _context.Guides não é nulo antes de usar Any()
        return (_context.Guides?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}