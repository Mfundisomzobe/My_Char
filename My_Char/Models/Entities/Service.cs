using System.ComponentModel.DataAnnotations;

namespace My_Char.Models.Entities
{
    public class Service
    {
        public int Id { get; set; }
        [Required, StringLength(80)]
        public string Name { get; set; } = "";
        [Range(5, 480)]
        public int DurationMinutes { get; set; }
        [Range(0, 100000)]
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
