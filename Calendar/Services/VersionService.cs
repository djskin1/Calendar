using System.Reflection;

namespace Calendar.Services
{
    public static class VersionService
    {
        public static string CurrentVersion
        {
            get
            {
                Version? version =
                    Assembly.GetExecutingAssembly()
                        .GetName()
                        .Version;

                if (version == null)
                {
                    return "Unknown";
                }

                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }
    }
}