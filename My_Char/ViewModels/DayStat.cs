namespace My_Char.ViewModels
{
    public class DayStat
    {
        public string Day { get; set; } = "";
        public int Total { get; set; }
        public int NoShows { get; set; }
        public double NoShowRate => Total == 0 ? 0 : Math.Round(NoShows * 100.0 / Total, 0);

    }
}
