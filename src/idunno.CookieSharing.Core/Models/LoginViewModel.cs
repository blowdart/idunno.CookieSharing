using System.ComponentModel.DataAnnotations;

namespace idunno.CookieSharing.Core
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
    }
}
