using System;
using System.Xml;

namespace Imageflow.Bindings
{
    public class VersionInfo
    {
        internal VersionInfo() { }
        internal static VersionInfo FromDynamic(dynamic versionInfo)
        {
            var info = new VersionInfo();

            info.BuildDate = XmlConvert.ToDateTime(versionInfo.build_date.Value, XmlDateTimeSerializationMode.Utc);
            info.LongVersionString = versionInfo.long_version_string.Value;
            info.LastGitCommit = versionInfo.last_git_commit.Value;
            info.DirtyWorkingTree = versionInfo.dirty_working_tree.Value;
            return info;
        }
        
        public string LongVersionString { get; private set; }
        public string LastGitCommit { get; private set; }
        public bool DirtyWorkingTree { get; private set; }
        public DateTimeOffset BuildDate { get; private set; }
    }
}