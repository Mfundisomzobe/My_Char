namespace My_Char.ViewModels
{
    public class CustomerBookVm
    {
        public string ShopName { get; set; } = "";
        public string Slug { get; set; } = "";
        public List<ServiceDto> Services { get; set; } = new();
    }
}
