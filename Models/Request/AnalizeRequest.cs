using System.ComponentModel.DataAnnotations;

namespace PatentsViewAPI.Models.Request
{
    public class AnalizeRequest
    {
        /// <summary>
        /// Optional application field to search in patent abstracts
        /// </summary>
        public string? Aplicacion { get; set; }

        /// <summary>
        /// Required array of keywords to search in patent abstracts
        /// </summary>
        [Required(ErrorMessage = "palabrasClaves is required")]
        [MinLength(1, ErrorMessage = "At least one keyword is required")]
        public List<string> PalabrasClaves { get; set; } = new();
    }
}
