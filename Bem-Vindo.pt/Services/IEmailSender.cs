// /Services/IEmailSender.cs
namespace Bem_vindo.pt.Services
{
    public interface IEmailSender
    {
        // Método que o nosso AccountController irá chamar
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}