namespace My_Char.ViewModels
{
    public class DashboardViewModel
    {
        public string BookingLink { get; set; } = "";
        public decimal MoneyMadeToday { get; set; }
        public decimal CashToday { get; set; }
        public decimal EftToday { get; set; }
        public decimal LostToNoShows { get; set; }
        public int NoShowsToday { get; set; }
        public List<DashboardAppt> Appointments { get; set; } = new();
    }
}
