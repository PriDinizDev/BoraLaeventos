using System;
using System.ComponentModel.DataAnnotations;

namespace Bem_Vindo.pt.Models
{
    public class Evento
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Descricao { get; set; }

        [Display(Name = "Data e Hora")]
        public DateTime DataHora { get; set; } = DateTime.Now;

        [Required, StringLength(160)]
        public string Local { get; set; } = string.Empty;

        [Display(Name = "Preço (€)")]
        [Range(0, 999999)]
        public decimal Preco { get; set; } = 0;

        [Display(Name = "Link para compra")]
        [Url]
        public string? LinkCompra { get; set; }
    }
}