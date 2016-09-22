using System.ComponentModel.DataAnnotations;

namespace idunno.CookieSharing.FullFramework
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
    }
}