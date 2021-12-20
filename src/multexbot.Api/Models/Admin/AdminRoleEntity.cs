using sp.Core.Constants;
using sp.Core.Exceptions;

namespace multexbot.Api.Models.Admin
{
    public class AdminRoleEntity
    {
        public AdminRoleEntity()
        {
        }

        public AdminRoleEntity(AdminRoleDto adminRoleDto)
        {
            Name = adminRoleDto.Name.ToUpper();
            Scopes = adminRoleDto.Scopes != null
                ? string.Join(AppConstants.Delimiter, adminRoleDto.Scopes)
                : throw new AppException("Scopes must be defined");
        }

        public string Name { get; set; }

        public string Scopes { get; set; }
    }
}