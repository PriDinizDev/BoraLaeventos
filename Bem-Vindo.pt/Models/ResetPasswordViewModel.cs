// /Models/ResetPasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace Bem_vindo.pt.Models
{
    public class ResetPasswordViewModel
    {
        // Precisamos do Id e do Token para saber quem estamos a alterar
        // Vamos passá-los para a View através de campos escondidos (hidden)
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Token { get; set; }


        [Required(ErrorMessage = "A nova password é obrigatória.")]
        [StringLength(100, ErrorMessage = "A {0} deve ter no mínimo {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nova Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nova Password")]
        [Compare("NewPassword", ErrorMessage = "A nova password e a confirmação não coincidem.")]
        public string ConfirmPassword { get; set; }
    }
}