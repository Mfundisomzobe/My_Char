using My_Char.Models.Entities;

namespace My_Char.ViewModels
{
    public class DashboardAppt
    {
        public int Id { get; set; }
        public DateTime StartLocal { get; set; }
        public string ClientName { get; set; } = "";
        public string ClientPhone { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public int DurationMin { get; set; }
        public decimal Price { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? PaymentMethod { get; set; }

    }
}
