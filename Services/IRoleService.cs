using Ticket.Models;

namespace Ticket.Services
{
    public interface IRoleService
    {
        bool HasRole(UserRole userRole, UserRole requiredRole);
        bool IsAdmin(UserRole userRole);
        bool IsOperator(UserRole userRole);
        bool IsViewer(UserRole userRole);
    }
}
