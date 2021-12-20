using System;
using System.ComponentModel.DataAnnotations;
using multexbot.Api.Constants;
using multexbot.Api.Models.Base;
using Newtonsoft.Json;

namespace multexbot.Api.Models.User
{
    public class VerificationEntity : BaseEntity
    {
        public string Username { get; set; }

        public string Email { get; set; }

        [Required] public string FirstName { get; set; }

        public string MiddleName { get; set; }

        [Required] public string LastName { get; set; }

        [Required] public DateTime DateOfBirth { get; set; }

        [Required] public string Nationality { get; set; }

        [Required] public string ResidentialAddress { get; set; }

        [Required] public string PostalCode { get; set; }

        [Required] public string City { get; set; }

        [Required] public DocumentType DocumentType { get; set; }

        [Required] public string DocumentNumber { get; set; }

        [Required] public string BackPhoto { get; set; }

        [Required] public string FrontPhoto { get; set; }

        [Required] public string AddressPhoto { get; set; }

        public VerifyStatus Status { get; set; }

        public string Note { get; set; }

        [JsonIgnore] public string SearchText { get; set; }
    }
}