using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace multexBot.Api.Models.Admin
{
    public class AdminDto
    {
        public AdminDto()
        {
            
        }
        public AdminDto(AdminEntity adminEntity, AdminRoleDto adminRole)
        {
            Id = adminEntity.Id;
            Email = adminEntity.Email;
            GaEnable = adminEntity.GaEnable;
            Role = adminRole.Name;
            Scopes = adminRole.Scopes;
            CreatedTime = adminEntity.CreatedTime;
        }

        public long Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public bool GaEnable { get; set; }

        [Required]
        public string Role { get; set; }

        public List<string> Scopes { get; set; }
        
        public long CreatedTime { get; set; }

        #region Response

        public bool ChangePasswordRequired { get; set; }

        #endregion

    }
}