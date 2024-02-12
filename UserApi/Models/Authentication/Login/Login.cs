using System.ComponentModel.DataAnnotations;

namespace UserApi.Models.Authentication.Login
{
    public class Login
    {
        [Required]
        public string? UserName { get; set; }
        [Required]
        public string? Password { get; set; }


    }
}
