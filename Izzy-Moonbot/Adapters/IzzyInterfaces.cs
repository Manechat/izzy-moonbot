using System.Collections.Generic;
using System.Threading.Tasks;

namespace Izzy_Moonbot.Adapters;

public interface IIzzyUser
{
    ulong Id { get; }
}

public interface IIzzyRole
{
    string Name { get; }
    ulong Id { get; }
}

public interface IIzzySocketMessageChannel
{
    ulong Id { get; }
    string Name { get; }
    Task<IIzzyUser> GetUserAsync(ulong userId);
}

public interface IIzzySocketTextChannel
{
    ulong Id { get; }
    string Name { get; }
    IReadOnlyCollection<IIzzyUser> Users { get; }
}

public interface IIzzyGuild
{
    ulong Id { get; }
    Task<IReadOnlyCollection<IIzzyUser>> SearchUsersAsync(string userSearchQuery);
    IReadOnlyCollection<IIzzySocketTextChannel> TextChannels { get; }
    IReadOnlyCollection<IIzzyRole> Roles { get; }
}

public interface IIzzyClient
{
    IIzzyUser CurrentUser { get; }
    IReadOnlyCollection<IIzzyGuild> Guilds { get; }
}

public interface IIzzyContext
{
    bool IsPrivate { get; }
    IIzzyGuild Guild { get; }
    IIzzyClient Client { get; }
    IIzzySocketMessageChannel Channel { get; }
}

