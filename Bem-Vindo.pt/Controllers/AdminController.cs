using Bem_vindo.pt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using Rotativa.AspNetCore;
using System.Text; // Adicionado para Encoding
using System.Xml.Linq;
using Bem_Vindo.pt.Models; // Adicionado para XDocument (Gerar XML)

namespace Bem_vindo.pt.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Admin/Users
    [HttpGet]
    [Route("Admin/Users")]
    [Route("Admin")]
    [Route("Admin/Index")]
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users.OrderBy(u => u.Nome).ToListAsync();

        ViewBag.UserManagementMessage = TempData["UserManagementMessage"];
        ViewBag.UserManagementError = TempData["UserManagementError"];

        return View(users);
    }

    // ===== ACTION DE ESTATÍSTICAS ATUALIZADA =====
    // GET: /Admin/Statistics
    [HttpGet]
    public async Task<IActionResult> Statistics()
    {
        var statistics = new StatisticsViewModel();
        statistics.TotalUsers = await _context.Users.CountAsync();
        statistics.ActiveUsers = await _context.Users.CountAsync(u => u.IsContaAtivada);
        statistics.InactiveUsers = statistics.TotalUsers - statistics.ActiveUsers;
        statistics.TotalGuides = await _context.Guides.CountAsync();
        statistics.RecentUsers = await _context.Users
            .OrderByDescending(u => u.Id)
            .Take(5)
            .ToListAsync();
        statistics.TotalForumTopics = await _context.Topics.CountAsync();
        statistics.TotalForumReplies = await _context.Replies.CountAsync();

        // Obter os 5 Guias Mais Favoritados
        statistics.TopFavoritedGuides = await _context.UserFavoriteGuides
            .GroupBy(ufg => ufg.GuideId) // <--- ERRO CORRIGIDO AQUI (era uFG)
            .Select(g => new { // Cria um objeto anónimo com o ID e a Contagem
                GuideId = g.Key,
                FavoriteCount = g.Count()
            })
            .OrderByDescending(x => x.FavoriteCount) // Ordena pela contagem
            .Take(5) // Pega os 5 primeiros
            .Join( // Junta com a tabela de Guias para obter o Título
                  _context.Guides,
                  favCount => favCount.GuideId, // Chave do resultado (favoritos)
                  guide => guide.Id, // Chave da tabela (Guias)
                  (favCount, guide) => new TopGuideInfo // Projeta para o nosso ViewModel
                  {
                      GuideId = guide.Id,
                      Title = guide.Titulo, // Usa a propriedade "Titulo" do Guia
                      FavoriteCount = favCount.FavoriteCount
                  })
            .ToListAsync();

        return View(statistics);
    }
    // ===== FIM DA ACTION ATUALIZADA =====


    // POST: /Admin/ActivateUser/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.IsContaAtivada = true;
            await _context.SaveChangesAsync();
            TempData["UserManagementMessage"] = $"Conta de {user.Nome} ativada com sucesso.";
        }
        else
        {
            TempData["UserManagementError"] = "Erro: Utilizador não encontrado.";
        }
        return RedirectToAction(nameof(Users));
    }

    // POST: /Admin/DeactivateUser/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.IsContaAtivada = false;
            await _context.SaveChangesAsync();
            TempData["UserManagementMessage"] = $"Conta de {user.Nome} desativada com sucesso.";
        }
        else
        {
            TempData["UserManagementError"] = "Erro: Utilizador não encontrado.";
        }
        return RedirectToAction(nameof(Users));
    }

    // GET: /Admin/PrintUsersPdf
    [HttpGet]
    public async Task<IActionResult> PrintUsersPdf()
    {
        var users = await _context.Users.OrderBy(u => u.Nome).ToListAsync();
        var pdfFileName = $"ListaUtilizadores_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

        return new ViewAsPdf("UsersPdf", users)
        {
            FileName = pdfFileName,
            PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
            PageSize = Rotativa.AspNetCore.Options.Size.A4,
            CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"10\""
        };
    }

    // ===== NOVA ACTION PARA GERAR O XML =====
    // GET: /Admin/ExportUsersToXml
    [HttpGet]
    public async Task<IActionResult> ExportUsersToXml()
    {
        // 1. Obter os dados (usar AsNoTracking para melhor performance de leitura)
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.Nome)
            .ToListAsync();

        // 2. Criar o documento XML em memória
        var xDocument = new XDocument(
            new XElement("Utilizadores", // Elemento raiz
                                         // Para cada utilizador na lista, criar um elemento "Utilizador"
                users.Select(user =>
                    new XElement("Utilizador",
                        new XAttribute("ID", user.Id), // ID como atributo
                        new XElement("Nome", user.Nome),
                        new XElement("Email", user.Email),
                        new XElement("Role", user.Role),
                        new XElement("ContaAtiva", user.IsContaAtivada)
                    )
                )
            )
        );

        // 3. Converter o documento XML para uma string e depois para bytes
        var xmlString = xDocument.ToString();
        var bytes = Encoding.UTF8.GetBytes(xmlString);

        // 4. Definir o nome do ficheiro
        var fileName = $"ListaUtilizadores_{DateTime.Now:yyyyMMdd_HHmmss}.xml";

        // 5. Retornar o ficheiro para download
        return File(bytes, "application/xml", fileName);
    }
    // ===== FIM DA NOVA ACTION =====
}