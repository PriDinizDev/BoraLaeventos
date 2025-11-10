// /Models/StatisticsViewModel.cs
using Bem_Vindo.pt.Models;
using System.Collections.Generic; // Para usar o List<>

namespace Bem_vindo.pt.Models
{
    // Esta classe auxiliar serve para guardar o resultado da nossa query
    // (Nome do Guia + Contagem de Favoritos)
    public class TopGuideInfo
    {
        public int GuideId { get; set; }
        public string Title { get; set; }
        public int FavoriteCount { get; set; }
    }


    // Este modelo não vai para a Base de Dados.
    // Serve apenas para organizar os dados para a página de Estatísticas.
    public class StatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int TotalGuides { get; set; }
        public int TotalForumTopics { get; set; } // Bónus: Total de Tópicos
        public int TotalForumReplies { get; set; } // Bónus: Total de Respostas

        // Uma lista dos 5 utilizadores mais recentes
        public List<User> RecentUsers { get; set; }

        // ----- NOVA PROPRIEDADE ADICIONADA AQUI -----
        // Uma lista dos 5 guias mais favoritados
        public List<TopGuideInfo> TopFavoritedGuides { get; set; }


        // Construtor para inicializar as listas
        public StatisticsViewModel()
        {
            RecentUsers = new List<User>();
            TopFavoritedGuides = new List<TopGuideInfo>();
        }
    }
}