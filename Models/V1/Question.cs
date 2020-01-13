using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace WebAPI.IVR.Models.V1
{
    public enum IVRQuestionnaireQuestionTypes
    {
        GetDigits = 1,
        GetRecording = 2
    }

    [DataContract]
    public class Question
    {
        [Required]
        [DataMember(Name = "questionid")]
        public int questionid { get; set; }

        [DataMember(Name = "answertype")]
        [EnumDataType(typeof(IVRQuestionnaireQuestionTypes))]
        public IVRQuestionnaireQuestionTypes answertype { get; set; }

        [DataMember(Name = "answersize")]
        public int answersize { get; set; }

        [DataMember(Name = "questiontext")]
        public string questiontext { get; set; }

        [DataMember(Name = "questionrecording")]
        public string questionrecording { get; set; }

        [DataMember(Name = "instructionrecording")]
        public string instructionrecording { get; set; }

        [DataMember(Name = "validationrecording")]
        public string validationrecording { get; set; }

        [DataMember(Name = "answerrecordingfilename")]
        public string answerrecordingfilename { get; set; }
    }
}
