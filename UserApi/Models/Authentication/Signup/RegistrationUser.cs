using System.ComponentModel.DataAnnotations;

namespace UserApi.Models.Authentication.Signup
{
    public class RegistrationUser
    {
        [Required]
        public string ? Email { get; set; }
        [Required]
        public string ? Password { get; set; }
        [Required]  
        public string ? UserName { get; set; }    
    }
}
