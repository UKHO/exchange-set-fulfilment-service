using System.Reflection;

namespace UKHO.ADDS.EFS.Orchestrator.Dashboard.Components.TypeBrowsers
{
    internal class PropertyView
    {
        public PropertyView(PropertyInfo propertyInfo)
        {
            PropertyInfo = PropertyInfo;
            Name = propertyInfo.Name;
            TypeName = propertyInfo.PropertyType.GetFriendlyName();
            IsComponentParameter = propertyInfo.IsComponentParameter();
        }

        public PropertyInfo PropertyInfo { get; }
        public string Name { get; private set; }
        public string TypeName { get; private set; }
        public bool IsComponentParameter { get; private set; }
    }
}
