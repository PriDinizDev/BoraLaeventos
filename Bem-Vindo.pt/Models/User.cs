using Bem_vindo.pt.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bem_Vindo.pt.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        [StringLength(256)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string? FotoPerfilUrl { get; set; }

        [Required]
        public string Role { get; set; } = "Participante";

        public bool IsContaAtivada { get; set; }

        public string? EmailConfirmationToken { get; set; }

        public DateTime? TokenGenerationTime { get; set; }

        public virtual ICollection<UserFavoriteGuide> FavoriteGuides { get; set; } = new HashSet<UserFavoriteGuide>();

        public User()
        {
            FavoriteGuides = new HashSet<UserFavoriteGuide>();
        }
    }
}