using CommunityToolkit.Mvvm.ComponentModel;

namespace Ticket.Models
{
    public partial class FilterChip : ObservableObject
    {
        public string Text { get; }

        [ObservableProperty]
        private bool _isSelected;

        public FilterChip(string text, bool isSelected = false)
        {
            Text = text;
            _isSelected = isSelected;
        }
    }
}
