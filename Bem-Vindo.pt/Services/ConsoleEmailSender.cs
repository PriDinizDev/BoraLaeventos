// /Services/ConsoleEmailSender.cs
using System.Diagnostics; // Importante para o Debug.WriteLine

namespace Bem_vindo.pt.Services
{
    // Esta classe "finge" que envia um email, mas apenas o escreve na consola
    // Repare que ela "herda" da interface IEmailSender que criámos
    public class ConsoleEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Usamos Debug.WriteLine para que a mensagem apareça na janela 
            // "Output" (Saída) do Visual Studio quando correr o projeto.
            Debug.WriteLine("===================================");
            Debug.WriteLine($"PARA: {email}");
            Debug.WriteLine($"ASSUNTO: {subject}");
            Debug.WriteLine("--- MENSAGEM ---");

            // Esta linha remove o HTML da mensagem para ser mais fácil ler na consola
            Debug.WriteLine(System.Text.RegularExpressions.Regex.Replace(htmlMessage, "<[^>]*>", ""));

            Debug.WriteLine("===================================");

            // Retorna uma tarefa completa, a dizer que o "envio" terminou.
            return Task.CompletedTask;
        }
    }
}