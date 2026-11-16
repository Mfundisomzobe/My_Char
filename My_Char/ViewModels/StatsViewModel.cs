namespace My_Char.ViewModels
{
    public class StatsViewModel
    {
        public string MonthLabel { get; set; } = "";
        public decimal TotalEarned { get; set; }
        public int TotalBookings { get; set; }
        public int NoShows { get; set; }
        public decimal LostToNoShows { get; set; }
        public List<ServiceStat> TopServices { get; set; } = new();
        public List<DayStat> ByDayOfWeek { get; set; } = new();

    }
}
