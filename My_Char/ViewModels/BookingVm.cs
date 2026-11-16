using My_Char.Models.Entities;

namespace My_Char.ViewModels
{
    public class BookingVm
    {
        public int Id { get; set; }
        public DateTime StartLocal { get; set; }
        public string ServiceName { get; set; } = "";
        public decimal Price { get; set; }
        public AppointmentStatus Status { get; set; }
        public bool CanCancel { get; set; }

    }
}
