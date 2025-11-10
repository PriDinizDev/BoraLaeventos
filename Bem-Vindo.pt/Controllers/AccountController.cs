// /Controllers/AccountController.cs

using Bem_vindo.pt.Models;
using Bem_vindo.pt.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Text;
using System.Web;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Bem_Vindo.pt.Models; // NECESSÁRIO para IdentityConstants

namespace Bem_vindo.pt.Controllers
{

    public class AccountController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher passwordHasher,
            IWebHostEnvironment webHostEnvironment,
            IEmailSender emailSender)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register() => View();

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model, string password)
        {
            ModelState.Remove("PasswordHash"); ModelState.Remove("FotoPerfilUrl"); ModelState.Remove("Role");
            ModelState.Remove("EmailConfirmationToken"); ModelState.Remove("TokenGenerationTime");
            ModelState.Remove("FavoriteGuides");
            if (!ModelState.IsValid) return View(model);

            var userExists = await _context.Users.AnyAsync(u => u.Email == model.Email);
            if (userExists) { ModelState.AddModelError("Email", "Este e-mail já está registado."); return View(model); }
            if (string.IsNullOrEmpty(password) || password.Length < 6) { ModelState.AddModelError("password", "A password é obrigatória e deve ter no mínimo 6 caracteres."); return View(model); }

            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            token = HttpUtility.UrlEncode(token);

            model.PasswordHash = _passwordHasher.HashPassword(password);
            model.Role = "Participante";
            model.IsContaAtivada = false;
            model.EmailConfirmationToken = token;
            model.TokenGenerationTime = DateTime.UtcNow;

            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = model.Id, token = token },
                Request.Scheme);

            if (confirmationLink == null)
            {
                _context.Users.Remove(model);
                await _context.SaveChangesAsync();
                ModelState.AddModelError("", "Erro fatal ao gerar o link de confirmação. O registo foi cancelado. Tente novamente.");
                return View(model);
            }

            var subject = "Bem-vindo.pt - Ative a sua conta";
            var message = $@"
                <h1>Bem-vindo ao Bem-vindo.pt!</h1>
                <p>Obrigado por se registar. Por favor, ative a sua conta clicando no link abaixo:</p>
                <p><a href='{confirmationLink}'>ATIVAR CONTA</a></p>
                <p>Se não foi você que se registou, por favor ignore este email.</p>
                <hr>
                <p>Link (para copiar e colar): {confirmationLink}</p>";

            await _emailSender.SendEmailAsync(model.Email, subject, message);

            TempData["SuccessMessage"] = "Registo efetuado! Enviámos um email de ativação para a sua conta. Por favor, verifique a sua caixa de entrada (e spam).";
            return RedirectToAction("RegistrationSuccess");
        }


        // GET: /Account/RegistrationSuccess
        [HttpGet]
        public IActionResult RegistrationSuccess()
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            if (ViewBag.SuccessMessage == null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }


        // GET: /Account/ConfirmEmail?userId=X&token=Y
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(int userId, string token)
        {
            if (userId == 0 || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Link de ativação inválido ou incompleto.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilizador não encontrado. O link pode ser antigo.";
                return RedirectToAction("Login");
            }

            if (user.IsContaAtivada)
            {
                TempData["SuccessMessage"] = "A sua conta já se encontra ativa. Pode fazer login.";
                return RedirectToAction("Login");
            }

            if (user.EmailConfirmationToken != token)
            {
                TempData["ErrorMessage"] = "Token de ativação inválido.";
                return RedirectToAction("Login");
            }

            if (user.TokenGenerationTime.HasValue && user.TokenGenerationTime.Value.AddHours(24) < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "O seu link de ativação expirou. Por favor, tente registar-se novamente.";
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Register");
            }

            user.IsContaAtivada = true;
            user.EmailConfirmationToken = null;
            user.TokenGenerationTime = null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Conta ativada com sucesso! Já pode fazer login.";
            return RedirectToAction("Login");
        }


        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            TempData["ReturnUrl"] = returnUrl;
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            ViewBag.ErrorMessage = TempData["ErrorMessage"];
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            returnUrl = returnUrl ?? TempData["ReturnUrl"] as string;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) { ModelState.AddModelError("", "Por favor, preencha o email e a password."); return View(); }

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || !_passwordHasher.VerifyPassword(password, user.PasswordHash ?? ""))
            {
                ModelState.AddModelError("", "Email ou password inválidos.");
                return View();
            }

            if (!user.IsContaAtivada)
            {
                ModelState.AddModelError("", "A sua conta ainda não foi ativada. Por favor, verifique o seu email (incluindo a caixa de spam) e clique no link de ativação.");
                return View();
            }

            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.Nome ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Participante")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                TempData.Remove("ReturnUrl");
                return Redirect(returnUrl);
            }
            else
            {
                TempData.Remove("ReturnUrl");
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }


        // GET: /Account/ManageProfile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ManageProfile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) { await HttpContext.SignOutAsync(); return RedirectToAction("Login"); }

            var user = await _context.Users
                .Include(u => u.FavoriteGuides)
                    .ThenInclude(fg => fg.Guide)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) { await HttpContext.SignOutAsync(); return RedirectToAction("Login"); }

            ViewBag.PasswordSuccessMessage = TempData["PasswordSuccessMessage"];
            ViewBag.PasswordErrorMessage = TempData["PasswordErrorMessage"];
            ViewBag.ProfileSuccessMessage = TempData["ProfileSuccessMessage"];
            return View(user);
        }

        // POST: /Account/ManageProfile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageProfile(User model, IFormFile? ProfileImageFile)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId) || model.Id != userId) { return Forbid(); }

            ModelState.Remove("PasswordHash"); ModelState.Remove("Role"); ModelState.Remove("IsContaAtivada");
            ModelState.Remove("FotoPerfilUrl");
            ModelState.Remove("EmailConfirmationToken"); ModelState.Remove("TokenGenerationTime");
            ModelState.Remove("FavoriteGuides");

            var userToUpdate = await _context.Users.FindAsync(userId);
            if (userToUpdate == null) { return NotFound("Utilizador não encontrado."); }

            // Validar email duplicado apenas se o email foi alterado
            if (userToUpdate.Email != model.Email)
            {
                var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "Este email já está a ser utilizado por outra conta.");
                }
            }

            // Validar Nome (não pode ser vazio)
            if (string.IsNullOrWhiteSpace(model.Nome))
            {
                ModelState.AddModelError("Nome", "O nome é obrigatório.");
            }


            // Bloco de Upload da Foto
            if (ModelState.IsValid && ProfileImageFile != null && ProfileImageFile.Length > 0)
            {
                // Validações (tamanho, tipo)
                if (ProfileImageFile.Length > 2 * 1024 * 1024) { ModelState.AddModelError("", "O tamanho máximo da imagem é 2MB."); }
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(ProfileImageFile.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension)) { ModelState.AddModelError("", "Formato de imagem inválido (JPG, PNG, GIF)."); }

                if (ModelState.IsValid) // Procede com upload só se não houver erros de validação da imagem
                {
                    try
                    {
                        var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profile_pics");
                        Directory.CreateDirectory(uploadsFolder);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Apagar ficheiro antigo ANTES de guardar o novo
                        if (!string.IsNullOrEmpty(userToUpdate.FotoPerfilUrl))
                        {
                            var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, userToUpdate.FotoPerfilUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath)) { try { System.IO.File.Delete(oldFilePath); } catch { /* Ignora erros ao apagar */ } }
                        }

                        using (var fileStream = new FileStream(filePath, FileMode.Create)) { await ProfileImageFile.CopyToAsync(fileStream); }
                        userToUpdate.FotoPerfilUrl = $"/images/profile_pics/{uniqueFileName}";
                    }
                    catch (IOException ioEx)
                    {
                        // Log ioEx
                        ModelState.AddModelError("", "Ocorreu um erro ao guardar a imagem. Tente novamente.");
                    }
                    catch (Exception ex) // Outras exceções inesperadas no upload
                    {
                        // Log ex
                        ModelState.AddModelError("", "Ocorreu um erro inesperado no upload da imagem.");
                    }
                }
            } // Fim do Bloco de Upload


            // Tenta guardar Nome/Email APENAS se não houver erros ATÉ AGORA
            if (ModelState.IsValid)
            {
                try
                {
                    userToUpdate.Nome = model.Nome; // Atualiza o nome
                    // Verifica novamente se o email pode ser atualizado
                    if (userToUpdate.Email != model.Email && !await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId))
                    {
                        userToUpdate.Email = model.Email; // Atualiza o email
                    }

                    await _context.SaveChangesAsync();
                    TempData["ProfileSuccessMessage"] = "Perfil atualizado com sucesso!";
                    return RedirectToAction(nameof(ManageProfile)); // Sucesso -> Redireciona
                }
                catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "Não foi possível guardar as alterações. Ocorreu um conflito de dados. Tente novamente."); }
                catch (DbUpdateException dbEx) { /* Log dbEx */ ModelState.AddModelError("", "Ocorreu um erro ao guardar na base de dados."); }
                catch (Exception ex) { /* Log ex */ ModelState.AddModelError("", "Ocorreu um erro inesperado ao guardar o perfil."); }
            }

            // Se ModelState inválido (originalmente ou após erros de upload/save), recarrega os dados para a View
            var userForView = await _context.Users
               .Include(u => u.FavoriteGuides).ThenInclude(fg => fg.Guide)
               .AsNoTracking()
               .FirstOrDefaultAsync(u => u.Id == userId);

            if (userForView == null) return NotFound("Utilizador não encontrado após erro.");

            // Passar as mensagens de erro/sucesso da password via ViewBag/TempData
            ViewBag.PasswordSuccessMessage = TempData.Peek("PasswordSuccessMessage");
            ViewBag.PasswordErrorMessage = TempData.Peek("PasswordErrorMessage");

            return View(userForView); // Retorna a View com os erros do ModelState e os dados recarregados
        }


        // POST: /Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword)) { TempData["PasswordErrorMessage"] = "Todos os campos são obrigatórios."; return RedirectToAction("ManageProfile"); }
            if (NewPassword.Length < 6) { TempData["PasswordErrorMessage"] = "A nova password deve ter no mínimo 6 caracteres."; return RedirectToAction("ManageProfile"); }
            if (NewPassword != ConfirmPassword) { TempData["PasswordErrorMessage"] = "A nova password e a confirmação não coincidem."; return RedirectToAction("ManageProfile"); }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) { await HttpContext.SignOutAsync(); return RedirectToAction("Login"); }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) { await HttpContext.SignOutAsync(); return RedirectToAction("Login"); }

            if (user.PasswordHash == null || !_passwordHasher.VerifyPassword(CurrentPassword, user.PasswordHash)) { TempData["PasswordErrorMessage"] = "A password atual está incorreta."; return RedirectToAction("ManageProfile"); }

            user.PasswordHash = _passwordHasher.HashPassword(NewPassword);
            await _context.SaveChangesAsync();

            TempData["PasswordSuccessMessage"] = "Password alterada com sucesso!";
            return RedirectToAction("ManageProfile");
        }

        // POST: /Account/DeleteAccount
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) { await HttpContext.SignOutAsync(); return RedirectToAction("Login"); }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) { await HttpContext.SignOutAsync(); return RedirectToAction("Login"); }

            try
            {
                // Apagar foto de perfil, se existir
                if (!string.IsNullOrEmpty(user.FotoPerfilUrl))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, user.FotoPerfilUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath)) { try { System.IO.File.Delete(filePath); } catch { /* Ignora erros */ } }
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["GlobalSuccessMessage"] = "A sua conta foi apagada com sucesso.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Log ex
                TempData["GlobalErrorMessage"] = "Ocorreu um erro ao apagar a sua conta.";
                return RedirectToAction("ManageProfile");
            }
        }

        // GET: /Account/MakeAdmin
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> MakeAdmin(string email)
        {
            if (string.IsNullOrEmpty(email)) return BadRequest("Forneça um email na query string (?email=...).");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound($"Utilizador com email '{email}' não encontrado.");
            user.Role = "Administrador";
            user.IsContaAtivada = true;
            await _context.SaveChangesAsync();
            if (User.Identity != null && User.Identity.IsAuthenticated && User.FindFirstValue(ClaimTypes.Email) == email) await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Content($"Utilizador '{email}' foi promovido a Administrador e ativado.");
        }


        // ----- NOVOS MÉTODOS PARA LOGIN SOCIAL (CHALLENGE) -----

        // POST: /Account/ExternalLogin?provider=Google&returnUrl=/
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            return Challenge(properties, provider);
        }

        // GET: /Account/ExternalLoginCallback?returnUrl=/
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            // 1. Lidar com Erros do Provedor (Google)
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Erro do provedor: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            // 2. Obter Informações do Google
            var info = await HttpContext.AuthenticateAsync(
                "Google"); // CORRIGIDO: Usar "Google" em vez de IdentityConstants.ExternalScheme

            if (info?.Principal == null)
            {
                TempData["ErrorMessage"] = "Não foi possível obter informações do provedor externo.";
                return RedirectToAction(nameof(Login));
            }

            // 3. Obter Email e Nome do Google
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "O Google não forneceu um endereço de email válido. O login falhou.";
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
                return RedirectToAction(nameof(Login));
            }

            // 4. Procurar o Utilizador no nosso sistema
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // ** CASO A: NOVO UTILIZADOR - REGISTAR NOVO PERFIL **
                var newUser = new User
                {
                    Nome = name,
                    Email = email,
                    Role = "Participante",
                    IsContaAtivada = true,
                    PasswordHash = "EXTERNAL_LOGIN" // Placeholder
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                user = newUser;
            }

            // 5. Fazer Login (SignIn) no nosso sistema
            var claims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.Nome ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Participante")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                Items = { { "LoginProvider", "Google" } }
            };

            // Fazer o Login no nosso sistema (com o nosso esquema de Cookies)
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 6. Limpar o esquema temporário do Google e redirecionar
            // O SignOut do esquema externo é opcional
            // await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); // Não usado para evitar o crash anterior

            // Redireciona para o returnUrl ou para a Home
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // ----- FIM DOS MÉTODOS PARA LOGIN SOCIAL -----


        // ----- MÉTODOS PARA RECUPERAÇÃO DE PASSWORD (SEM ALTERAÇÕES) -----
        [HttpGet][AllowAnonymous] public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("email", "Por favor, insira o seu endereço de email.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            token = HttpUtility.UrlEncode(token);

            user.EmailConfirmationToken = token;
            user.TokenGenerationTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { userId = user.Id, token = token },
                Request.Scheme);

            if (resetLink == null)
            {
                ModelState.AddModelError("", "Erro fatal ao gerar o link de recuperação. Tente novamente.");
                return View(); // Retorna a view de ForgotPassword com o erro
            }

            var subject = "Bem-vindo.pt - Recuperação de Password";
            var message = $@"
                <h1>Recuperação de Password</h1>
                <p>Recebemos um pedido para redefinir a sua password. Se foi você, clique no link abaixo:</p>
                <p><a href='{resetLink}'>REDEFINIR PASSWORD</a></p>
                <p>Este link é válido por 1 hora.</p>
                <p>Se não foi você que fez este pedido, por favor ignore este email.</p>
                <hr>
                <p>Link (para copiar e colar): {resetLink}</p>";

            await _emailSender.SendEmailAsync(user.Email, subject, message);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet][AllowAnonymous] public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(int userId, string token)
        {
            if (userId == 0 || string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Link de recuperação inválido ou incompleto.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null || user.EmailConfirmationToken != token || !user.TokenGenerationTime.HasValue || user.TokenGenerationTime.Value.AddHours(1) < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Link de recuperação inválido ou expirado. Por favor, tente novamente.";
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            ModelState.Remove("UserId");
            ModelState.Remove("Token");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FindAsync(model.UserId);

            if (user == null || user.EmailConfirmationToken != model.Token || !user.TokenGenerationTime.HasValue || user.TokenGenerationTime.Value.AddHours(1) < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Link de recuperação inválido ou expirado. Por favor, tente novamente.";
                return RedirectToAction("ForgotPassword");
            }

            user.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);
            user.EmailConfirmationToken = null;
            user.TokenGenerationTime = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password redefinida com sucesso! Já pode fazer login com a sua nova password.";
            return RedirectToAction("Login");
        }

    } // Fim da classe AccountController
} // Fim do namespace