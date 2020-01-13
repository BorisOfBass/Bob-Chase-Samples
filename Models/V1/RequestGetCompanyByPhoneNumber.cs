using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace WebAPI.IVR.Models.V1
{
    [DataContract]
    public class RequestGetCompanyByPhoneNumber
    {
        [Required]
        [DataMember(Name = "authentication")]
        public Authentication authentication { get; set; }

        [Required]
        [DataMember(Name = "applicant")]
        public RequestGetCompanyByPhoneNumberApplicant applicant { get; set; }

    }

    public class RequestGetCompanyByPhoneNumberApplicant : ApplicantIdentifiers
    {
        [Required]
        [DataMember(Name = "phonenumber")]
        public string phonenumber { get; set; }

        [DataMember(Name = "languageid")]
        public string languageid { get; set; }
    }
}
