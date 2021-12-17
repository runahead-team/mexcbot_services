using System;
using System.Collections.Generic;
using System.Linq;
using sp.Core.Constants;

namespace multexBot.Api.Models.Admin
{
    public class AdminRoleDto
    {
        public AdminRoleDto()
        {
            
        }
        
        public AdminRoleDto(AdminRoleEntity adminRoleEntity)
        {
            Name = adminRoleEntity.Name;
            Scopes = adminRoleEntity.Scopes?.Split(AppConstants.Delimiter, StringSplitOptions.RemoveEmptyEntries)
                .ToList() ?? new List<string>();
        }

        public string Name { get; set; }

        public List<string> Scopes { get; set; }
    }
}