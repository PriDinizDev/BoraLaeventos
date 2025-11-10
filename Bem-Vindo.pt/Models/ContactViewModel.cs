// /Models/ContactViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Bem_vindo.pt.Models
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Por favor, insira o seu nome.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        [Display(Name = "O seu Nome")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "Por favor, insira o seu email.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        [Display(Name = "O seu Email (para respondermos)")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Por favor, insira um assunto.")]
        [StringLength(150, ErrorMessage = "O assunto não pode exceder 150 caracteres.")]
        [Display(Name = "Assunto")]
        public string Assunto { get; set; }

        [Required(ErrorMessage = "Por favor, escreva a sua mensagem.")]
        [StringLength(5000, ErrorMessage = "A mensagem não pode exceder 5000 caracteres.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Mensagem")]
        public string Mensagem { get; set; }
    }
}