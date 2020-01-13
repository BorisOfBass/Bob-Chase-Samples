using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace WebAPI.IVR.Models.V1
{
    [DataContract]
    public class Authentication
    {
        [Required]
        [DataMember(Name = "username")]
        public string username { get; set; }

        [Required]
        [DataMember(Name = "password")]
        public string password { get; set; }
    }
}
