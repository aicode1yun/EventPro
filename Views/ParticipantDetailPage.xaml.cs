using Ticket.ViewModels;

namespace Ticket.Views
{
    public partial class ParticipantDetailPage : ContentPage
    {
        public ParticipantDetailPage(ParticipantDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
