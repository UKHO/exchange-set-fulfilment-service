using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace UKHO.ADDS.EFS.Extensions
{
    internal static class EnumMetadataExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var (display, description, hasDisplay, _) = GetAttributes(value);
            if (hasDisplay && !string.IsNullOrWhiteSpace(display!.GetName()))
            {
                return display!.GetName()!;
            }

            return value.ToString();
        }

        public static string? GetDisplayDescription(this Enum value)
        {
            var (display, description, hasDisplay, hasDescription) = GetAttributes(value);

            if (hasDisplay && !string.IsNullOrWhiteSpace(display!.GetDescription()))
            {
                return display!.GetDescription();
            }

            if (hasDescription && !string.IsNullOrWhiteSpace(description!.Description))
            {
                return description!.Description;
            }

            return null;
        }

        private static (DisplayAttribute? display, DescriptionAttribute? description, bool hasDisplay, bool hasDescription)
            GetAttributes(Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            if (member is null)
            {
                return (null, null, false, false);
            }

            var display = member.GetCustomAttribute<DisplayAttribute>(false);
            var desc = member.GetCustomAttribute<DescriptionAttribute>(false);

            return (display, desc, display is not null, desc is not null);
        }
    }
}
