using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BetterOtherRoles.Modules.Twitch;

public enum TwitchScope
{
    None,
    [Description("channel:manage:broadcast")]
    ChannelManageBroadcast,
}

public static class EnumExtensions
{
    public static string GetDescription(this Enum e)
    {
        var attribute =
            e.GetType()
                    .GetTypeInfo()
                    .GetMember(e.ToString())
                    .FirstOrDefault(member => member.MemberType == MemberTypes.Field)
                    ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    .SingleOrDefault()
                as DescriptionAttribute;

        return attribute?.Description ?? e.ToString();
    }
}