// /Models/Topic.cs
using Bem_Vindo.pt.Models;
using System;
using System.Collections.Generic; // Para usar ICollection<>
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Para usar [ForeignKey]

namespace Bem_vindo.pt.Models
{
    public class Topic
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O título do tópico é obrigatório.")]
        [StringLength(150, ErrorMessage = "O título não pode exceder 150 caracteres.")]
        [Display(Name = "Título do Tópico")]
        public string Title { get; set; }

        [Required(ErrorMessage = "O conteúdo inicial é obrigatório.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Conteúdo")]
        public string Content { get; set; } // Conteúdo da primeira mensagem

        [Required]
        [Display(Name = "Data de Criação")]
        public DateTime CreatedAt { get; set; }

        // --- Relação com o Utilizador (Quem criou o tópico?) ---
        [Required]
        public int UserId { get; set; } // Chave estrangeira para a tabela Users

        [ForeignKey("UserId")]
        public virtual User Author { get; set; } // Propriedade de navegação

        // --- Relação com as Respostas (Um tópico tem muitas respostas) ---
        public virtual ICollection<Reply> Replies { get; set; }

        // Construtor para inicializar a lista de respostas e a data
        public Topic()
        {
            Replies = new HashSet<Reply>();
            CreatedAt = DateTime.UtcNow; // Define a data de criação automaticamente
        }
    }
}