using System.Runtime.Serialization;

namespace WebAPI.IVR.Models
{
    [DataContract(Name = "ApplicantIdentifiers")]
    public class ApplicantIdentifiers
    {
        [DataMember(Name = "externalid")]
        public string externalid { get; set; }
    }
}
