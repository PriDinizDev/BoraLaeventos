// /Models/Reply.cs
using Bem_Vindo.pt.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Para usar [ForeignKey]

namespace Bem_vindo.pt.Models
{
    public class Reply
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O conteúdo da resposta é obrigatório.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Conteúdo da Resposta")]
        public string Content { get; set; }

        [Required]
        [Display(Name = "Data de Publicação")]
        public DateTime CreatedAt { get; set; }

        // --- Relação com o Utilizador (Quem escreveu a resposta?) ---
        [Required]
        public int UserId { get; set; } // Chave estrangeira para a tabela Users

        [ForeignKey("UserId")]
        public virtual User Author { get; set; } // Propriedade de navegação

        // --- Relação com o Tópico (A que tópico pertence esta resposta?) ---
        [Required]
        public int TopicId { get; set; } // Chave estrangeira para a tabela Topics

        [ForeignKey("TopicId")]
        public virtual Topic ParentTopic { get; set; } // Propriedade de navegação

        // Construtor para definir a data
        public Reply()
        {
            CreatedAt = DateTime.UtcNow; // Define a data de criação automaticamente
        }
    }
}