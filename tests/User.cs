namespace Kothf.Data.Tests;

/// <summary>
/// User entity
/// </summary>
public sealed class User
{
    /// <summary>
    /// User code
    /// </summary>
    public string? UserCode { get; set; }

    /// <summary>
    /// User name
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Phone
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Attributes flags
    /// </summary>
    public int? Attributes { get; set; }

    /// <summary>
    /// State
    /// </summary>
    public int? State { get; set; }

    /// <summary>
    /// Time of creation
    /// </summary>
    public DateTime? CreatedTime { get; set; }

    /// <summary>
    /// Time of last modification
    /// </summary>
    public DateTime? LastModifiedTime { get; set; }

    /// <summary>
    /// Returns a string that represents the current user, including key property values.
    /// </summary>
    public override string ToString()
    {
        return $"UserCode: {UserCode}, UserName: {UserName}, Email: {Email}, Phone: {Phone}, State: {State}, CreatedTime: {CreatedTime}, LastModifiedTime: {LastModifiedTime}";
    }
}
