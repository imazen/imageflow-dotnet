using System.Xml;

namespace Imageflow.Bindings
{
    public class VersionInfo
    {
        private VersionInfo(dynamic versionInfo)
        {
            LongVersionString = versionInfo.long_version_string.Value;
            LastGitCommit = versionInfo.last_git_commit.Value;
            DirtyWorkingTree = versionInfo.dirty_working_tree.Value;
            GitTag = versionInfo.git_tag?.Value;
            GitDescribeAlways = versionInfo.git_describe_always?.Value;
            // Sometimes Newtonsoft gives us a DateTime; other times a string.
            object dateTime = versionInfo.build_date.Value;
            if (dateTime is string time)
            {
                BuildDate = XmlConvert.ToDateTime(time, XmlDateTimeSerializationMode.Utc);
            }
            else if (dateTime is DateTime dt)
            {
                BuildDate = new DateTimeOffset(dt);
            }
        }
        internal static VersionInfo FromDynamic(dynamic versionInfo)
        {
            return new VersionInfo(versionInfo);
        }
        
        public string LongVersionString { get; private set; }
        public string LastGitCommit { get; private set; }
        
        /// <summary>
        /// Usually includes the last version tag, the number of commits since, and a shortened git hash
        /// </summary>
        public string? GitDescribeAlways { get; private set; }
        
        /// <summary>
        /// May be null if the current version was not a tagged release
        /// </summary>
        public string? GitTag { get; private set; }
        public bool DirtyWorkingTree { get; private set; }
        public DateTimeOffset BuildDate { get; private set; }
    }
}