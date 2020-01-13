using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace WebAPI.IVR.Models.V1
{
    [DataContract]
    public class RequestNewApplicant
    {
        [Required]
        [DataMember(Name = "authentication")]
        public Authentication authentication { get; set; }

        [Required]
        [DataMember(Name = "applicant")]
        public NewApplicantApplicant applicant { get; set; }
    }


    [DataContract]
    public class NewApplicantApplicant : ApplicantIdentifiers
    {
        [Required, MinLength(9), MaxLength(9)]
        [DataMember(Name = "ssn")]
        public string ssn { get; set; }

        [Required, MinLength(5), MaxLength(5)]
        [DataMember(Name = "zip")]
        public string zip { get; set; }

        [Required]
        [DataMember(Name = "under40")]
        public bool under40 { get; set; }

        [DataMember(Name = "birthdate")]
        //[DataType(DataType.Date)]
        public DateTime birthdate { get; set; }
    }
}
