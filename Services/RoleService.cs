using Ticket.Models;

namespace Ticket.Services
{
    public class RoleService : IRoleService
    {
        public bool HasRole(UserRole userRole, UserRole requiredRole)
        {
            // Admin can do everything
            if (userRole == UserRole.Admin)
                return true;

            // Check if user has the required role or higher
            return userRole >= requiredRole;
        }

        public bool IsAdmin(UserRole userRole)
        {
            return userRole == UserRole.Admin;
        }

        public bool IsOperator(UserRole userRole)
        {
            return userRole >= UserRole.Operator;
        }

        public bool IsViewer(UserRole userRole)
        {
            return userRole >= UserRole.Viewer;
        }
    }
}
