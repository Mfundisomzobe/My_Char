using System.ComponentModel.DataAnnotations;

namespace My_Char.Models.Entities
{
    public class Client
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string FullName { get; set; } = "";
        [Required, StringLength(15)]
        public string Phone { get; set; } = "";
        public string? Notes { get; set; }
        public int VisitCount { get; set; }
        public int NoShowCount { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    }
}
