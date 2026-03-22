namespace LaborPL.Models
{
    public class StatCardViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string ColorClass { get; set; } = "primary";
        public bool IsLightText { get; set; } = true;

        public StatCardViewModel(string title, string value, string iconClass, string colorClass = "primary", bool isLightText = true)
        {
            Title = title;
            Value = value;
            IconClass = iconClass;
            ColorClass = colorClass;
            IsLightText = isLightText;
        }
    }
}
