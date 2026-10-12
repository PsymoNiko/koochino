namespace MyShop.Web.Models;

/// <summary>
/// One-time sign-in data passed from Blazor login to Razor Page via cache (avoids session write after response started).
/// </summary>
public class PendingSignInData
{
    public string UserId { get; set; } = "";
    public string Role { get; set; } = "";
    public string Username { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool RememberMe { get; set; } = true;
}
