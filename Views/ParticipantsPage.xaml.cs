using Ticket.ViewModels;

namespace Ticket.Views
{
    public partial class ParticipantsPage : ContentPage
    {
        private readonly ParticipantsViewModel _viewModel;

        public ParticipantsPage(ParticipantsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.LoadAttendeesCommand.Execute(null);
        }
    }
}
