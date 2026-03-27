using System.ComponentModel.DataAnnotations;

namespace Client.Models.Requests;

public sealed class UpsertUserRequest
{
    public Guid? Id { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;
}
