using Ticket.ViewModels;

namespace Ticket.Views
{
    public partial class TicketPreviewPage : ContentPage
    {
        public TicketPreviewPage(TicketPreviewViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
