using System.ComponentModel.DataAnnotations;


namespace idunno.CookieSharing.CoreV2.Models
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
    }
}

