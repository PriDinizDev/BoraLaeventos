// /Controllers/ForumController.cs
using Bem_vindo.pt.Models;
using Bem_Vindo.pt.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bem_vindo.pt.Controllers
{
    [Authorize]
    public class ForumController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ForumController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Forum ou /Forum/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var topics = await _context.Topics
                .Include(t => t.Author)
                .Include(t => t.Replies)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(topics);
        }

        // GET: /Forum/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var topic = await _context.Topics
                .Include(t => t.Author)
                .Include(t => t.Replies)
                    .ThenInclude(r => r.Author)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topic == null)
            {
                return NotFound();
            }

            topic.Replies = topic.Replies.OrderBy(r => r.CreatedAt).ToList();

            // Mensagem de erro vinda do DeleteReply (se houver)
            ViewBag.ReplyError = TempData["ReplyError"];

            return View(topic);
        }

        // GET: /Forum/CreateTopic
        [HttpGet]
        public IActionResult CreateTopic()
        {
            return View();
        }

        // POST: /Forum/CreateTopic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTopic(Topic topic)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("Author");
            ModelState.Remove("Replies");
            ModelState.Remove("CreatedAt");

            if (ModelState.IsValid)
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId))
                {
                    topic.UserId = userId;
                    topic.CreatedAt = DateTime.UtcNow;

                    _context.Topics.Add(topic);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Details", new { id = topic.Id });
                }
                else
                {
                    ModelState.AddModelError("", "Erro ao identificar o utilizador. Tente fazer logout e login novamente.");
                }
            }
            return View(topic);
        }

        // POST: /Forum/AddReply 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReply(int topicId, string replyContent)
        {
            if (topicId == 0 || string.IsNullOrWhiteSpace(replyContent))
            {
                TempData["ReplyError"] = "O conteúdo da resposta não pode estar vazio.";
                return RedirectToAction("Details", new { id = topicId });
            }

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == topicId);
            if (!topicExists)
            {
                return NotFound("Tópico não encontrado.");
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                var reply = new Reply
                {
                    Content = replyContent,
                    CreatedAt = DateTime.UtcNow,
                    UserId = userId,
                    TopicId = topicId
                };

                _context.Replies.Add(reply);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", new { id = topicId });
            }
            else
            {
                TempData["ReplyError"] = "Erro ao identificar o utilizador.";
                return RedirectToAction("Details", new { id = topicId });
            }
        }

        // ===== NOVA ACTION PARA EXCLUIR RESPOSTA =====
        // POST: /Forum/DeleteReply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            if (replyId == 0)
            {
                return BadRequest("ID da resposta inválido.");
            }

            // Encontrar a resposta E o tópico pai (para saber para onde redirecionar)
            var reply = await _context.Replies
                .Include(r => r.ParentTopic) // Incluir o tópico pai
                .FirstOrDefaultAsync(r => r.Id == replyId);

            if (reply == null || reply.ParentTopic == null) // Verifica se a resposta E o tópico existem
            {
                // Mesmo que a resposta não exista, tentamos redirecionar para algum lugar
                // Podemos tentar ir para o Index do Fórum se não soubermos o TopicId
                TempData["ReplyError"] = "Erro: Resposta não encontrada.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar permissões: O utilizador logado é o autor OU é Administrador?
            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(currentUserIdString, out int currentUserId);

            if (reply.UserId == currentUserId || User.IsInRole("Administrador"))
            {
                // Tem permissão, pode excluir
                _context.Replies.Remove(reply);
                await _context.SaveChangesAsync();
                TempData["ReplySuccess"] = "Resposta excluída com sucesso."; // Mensagem opcional
                return RedirectToAction("Details", new { id = reply.TopicId }); // Volta para o tópico
            }
            else
            {
                // Não tem permissão
                TempData["ReplyError"] = "Não tem permissão para excluir esta resposta.";
                return RedirectToAction("Details", new { id = reply.TopicId }); // Volta para o tópico
            }
        }
        // ===== FIM DA NOVA ACTION =====

        // POST: /Forum/DeleteTopic
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTopic(int topicId)
        {
            if (topicId == 0)
            {
                return BadRequest("ID do tópico inválido.");
            }

            var topic = await _context.Topics
                .Include(t => t.Replies)
                .FirstOrDefaultAsync(t => t.Id == topicId);

            if (topic == null)
            {
                TempData["GlobalErrorMessage"] = "Tópico não encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(currentUserIdString, out int currentUserId);

            if (topic.UserId == currentUserId || User.IsInRole("Administrador"))
            {
                _context.Topics.Remove(topic);
                await _context.SaveChangesAsync();
                TempData["GlobalSuccessMessage"] = "Tópico excluído com sucesso.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["GlobalErrorMessage"] = "Não tem permissão para excluir este tópico.";
                return RedirectToAction("Details", new { id = topicId });
            }
        }
    }
}
        
    