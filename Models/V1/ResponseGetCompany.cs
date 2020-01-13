using System.Runtime.Serialization;

namespace WebAPI.IVR.Models.V1
{
    public enum ResponseGetCompanyActions
    {
        PlayCompanyRecording = 1,
        PlayCloseRecording = 2,
        RedirectToCSR = 3,
        AskOverrideCode = 4
    }

    [DataContract]
    public class ResponseGetCompany
    {
        [DataMember(Name = "companyid")]
        public int companyid { get; set; }

        [DataMember(Name = "action")]
        public ResponseGetCompanyActions action { get; set; }

        [DataMember(Name = "redirecturl")]
        public string redirecturl { get; set; }

        [DataMember(Name = "recordingfile")]
        public string recordingfile { get; set; }
    }
}
