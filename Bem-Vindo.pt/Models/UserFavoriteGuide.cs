// /Models/UserFavoriteGuide.cs
using Bem_Vindo.pt.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bem_vindo.pt.Models
{
    // Esta classe representa a tabela de junção (Join Table)
    // para a relação Muitos-para-Muitos entre User e Guide
    public class UserFavoriteGuide
    {
        // Chave estrangeira para User
        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }


        // Chave estrangeira para Guide (ou Guia)
        [Required]
        public int GuideId { get; set; }

        [ForeignKey("GuideId")]
        public virtual Guide Guide { get; set; } // Verifique se o nome "Guide" está correto

        // Opcional: Data em que foi favoritado
        public DateTime FavoritedAt { get; set; }

        public UserFavoriteGuide()
        {
            FavoritedAt = DateTime.UtcNow;
        }
    }
}