// /Models/Guide.cs
using System;
using System.Collections.Generic; // Para usar ICollection<>
using System.ComponentModel.DataAnnotations;

namespace Bem_vindo.pt.Models
{
    public class Guide
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(200)]
        public string Titulo { get; set; }

        [StringLength(100)]
        public string? Entidade { get; set; } // Ex: AIMA, Finanças

        [StringLength(250)]
        public string? Processo { get; set; } // Ex: Obter NIF, Renovação

        // ===== NOME CORRETO AQUI =====
        [Required(ErrorMessage = "O conteúdo é obrigatório.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Conteúdo Passo a Passo")]
        public string ConteudoPassoAPasso { get; set; } // Guia passo-a-passo

        // ===== NOME CORRETO AQUI =====
        [Url(ErrorMessage = "Por favor, insira um URL válido.")]
        [Display(Name = "Link Governo Oficial")]
        public string? LinkGovernoOficial { get; set; }

        // ----- PROPRIEDADE PARA OS UTILIZADORES QUE FAVORITARAM -----
        public virtual ICollection<UserFavoriteGuide> FavoritedByUsers { get; set; }

        // Construtor para inicializar a lista
        public Guide()
        {
            FavoritedByUsers = new HashSet<UserFavoriteGuide>();
        }
    }
}