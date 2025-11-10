// /Models/ErrorViewModel.cs
namespace Bem_vindo.pt.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public int? StatusCode { get; set; }
    }
}