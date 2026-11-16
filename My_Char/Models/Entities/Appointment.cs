using System.ComponentModel.DataAnnotations;

namespace My_Char.Models.Entities
{
    public class Appointment
    {

        public int Id { get; set; }
        public int ClientId { get; set; }
        public int ServiceId { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
        public decimal Price { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        [Timestamp]
        public byte[]? RowVersion { get; set; }
        public Client Client { get; set; } = null!;
        public Service Service { get; set; } = null!;

    }
    public enum AppointmentStatus
    {
        Booked, Confirmed, InProgress, Completed, Cancelled, NoShow
    }

}
