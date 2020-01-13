using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace WebAPI.IVR.Models.V1
{
    [DataContract]
    public class RequestGetCompanyByCode
    {
        [Required]
        [DataMember(Name = "authentication")]
        public Authentication authentication { get; set; }

        [Required]
        [DataMember(Name = "applicant")]
        public RequestGetCompanyByCodeApplicant applicant { get; set; }

    }

    public class RequestGetCompanyByCodeApplicant : ApplicantIdentifiers
    {
        [Required]
        [DataMember(Name = "phonenumber")]
        public string phonenumber { get; set; }

        [Required]
        [DataMember(Name = "overridecode")]
        public string overridecode { get; set; }

        [DataMember(Name = "languageid")]
        public string languageid { get; set; }
    }
}
