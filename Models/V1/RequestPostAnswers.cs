using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace WebAPI.IVR.Models.V1
{
    [DataContract]
    public class RequestPostAnswers
    {
        [Required]
        [DataMember(Name = "authentication")]
        public Authentication authentication { get; set; }

        [Required]
        [DataMember(Name = "applicant")]
        public PostAnswersApplicant applicant { get; set; }
    }

    [DataContract]
    public class PostAnswersApplicant : ApplicantIdentifiers
    {
        [Required]
        [DataMember(Name = "answers")]
        public PostAnswersAnswer[] answers { get; set; }
    }

    [DataContract]
    public class PostAnswersAnswer
    {
        [Required]
        [DataMember(Name = "questionid")]
        public int questionid { get; set; }

        [Required(AllowEmptyStrings = true)]
        [DataMember(Name = "value")]
        public string value { get; set; }
    }
}
