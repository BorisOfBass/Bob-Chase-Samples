using System.Runtime.Serialization;

namespace WebAPI.IVR.Models.V1
{
    [DataContract(Name = "errorresponse")]
    public class ResponseError
    {
        [DataMember(Name = "code")]
        public string code { get; set; }

        [DataMember(Name = "message")]
        public string message { get; set; }
    }
}
