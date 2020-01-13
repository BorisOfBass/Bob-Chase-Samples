using System.Runtime.Serialization;

namespace WebAPI.IVR.Models.V1
{
    [DataContract]
    public class ResponsePostAnswers
    {
        [DataMember(Name = "applicant")]
        public ResponseNewApplicantApplicant applicant { get; set; }
    }
}
