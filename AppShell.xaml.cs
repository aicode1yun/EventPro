using Ticket.Views;

namespace Ticket
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("ticketPreview", typeof(TicketPreviewPage));
            Routing.RegisterRoute("participantDetail", typeof(ParticipantDetailPage));
        }
    }
}
