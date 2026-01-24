using System.Data.Common;

namespace Kothf.Data.Tests;

public static class DbDataReaderExtensions
{
    /// <summary>
    /// Maps the columns of <see cref="DbDataReader"/> to the properties of <see cref="User"/>
    /// Order of mapping: UserCode,UserName,Password,Email,Phone,Attributes,[State],CreatedTime,LastModifiedTime
    /// </summary>
    public static User MapToUser(this DbDataReader source)
    {
        return new User {
            UserCode = !source.IsDBNull(0) ? source.GetString(0) : null,
            UserName = !source.IsDBNull(1) ? source.GetString(1) : null,
            Password = !source.IsDBNull(2) ? source.GetString(2) : null,
            Email = !source.IsDBNull(3) ? source.GetString(3) : null,
            Phone = !source.IsDBNull(4) ? source.GetString(4) : null,
            Attributes = !source.IsDBNull(5) ? source.GetInt32(5) : null,
            State = !source.IsDBNull(6) ? source.GetInt32(6) : null,
            CreatedTime = !source.IsDBNull(7) ? source.GetDateTime(7) : null,
            LastModifiedTime = !source.IsDBNull(8) ? source.GetDateTime(8) : null
        };
    }
}
