namespace Calendar.Models
{
    public class Role
    {
        public int Id { get; set; }

        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }

        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<RolePermission> RolePermissions { get; set; }
            = new List<RolePermission>();

        public ICollection<UserRole> UserRoles { get; set; }
            = new List<UserRole>();

        public ICollection<LocalAdministratorRole> LocalAdministratorRoles { get; set; }
            = new List<LocalAdministratorRole>();
    }
}