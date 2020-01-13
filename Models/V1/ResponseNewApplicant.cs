using System.Runtime.Serialization;

namespace WebAPI.IVR.Models.V1
{
    public enum IVRApplicantActions
    {
        AskQuestions = 1,
        PlayCloseRecording = 2,
        RedirectToCSR = 3,
        PlayThanksRecording = 4 // We need to talk about thanks messages
    }

    [DataContract]
    public class ResponseNewApplicant
    {
        [DataMember(Name = "applicant")]
        public ResponseNewApplicantApplicant applicant { get; set; }
    }

    [DataContract]
    public class ResponseNewApplicantApplicant
    {
        [DataMember(Name = "applicantid")]
        public int applicantid { get; set; }

        [DataMember(Name = "action")]
        public IVRApplicantActions action { get; set; }

        [DataMember(Name = "question")]
        public Question question { get; set; }

        [DataMember(Name = "redirecturl")]
        public string redirecturl { get; set; }

        [DataMember(Name = "recordingfile")]
        public string recordingfile { get; set; }

        [DataMember(Name = "registrationnumber")]
        public string registrationnumber { get; set; }
    }
}
