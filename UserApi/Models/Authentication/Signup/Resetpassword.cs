using System.ComponentModel.DataAnnotations;

namespace UserApi.Models.Authentication.Signup
{
    public class Resetpassword
    {
        [Required]
        public string ? Password { get; set; } = null;
        [Compare("Password", ErrorMessage ="This parrword das not match")]
        public string ? Confirmationpassword { get; set; }= null;
        public string? Email { get; set; } = null;
        public string? Token { get; set; } = null;

    }
}
