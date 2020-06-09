using System;
using System.Xml;

namespace Imageflow.Bindings
{
    public class VersionInfo
    {
        private VersionInfo() { }
        internal static VersionInfo FromDynamic(dynamic versionInfo)
        {
            var info = new VersionInfo();
            string longVersionString = versionInfo.long_version_string.Value;
            string lastGitCommit = versionInfo.last_git_commit.Value;
            bool dirtyWorkingTree = versionInfo.dirty_working_tree.Value;

            // Sometimes Newtonsoft gives us a DateTime, other times a string.
            object dateTime = versionInfo.build_date.Value;
            if (dateTime is string)
            {
                info.BuildDate = XmlConvert.ToDateTime(dateTime as string, XmlDateTimeSerializationMode.Utc);
            }
            else if (dateTime is DateTime)
            {
                info.BuildDate = new DateTimeOffset((dateTime as DateTime?).Value);
            }
            info.LongVersionString = longVersionString;
            info.LastGitCommit = lastGitCommit;
            info.DirtyWorkingTree = dirtyWorkingTree;
            return info;
        }
        
        public string LongVersionString { get; private set; }
        public string LastGitCommit { get; private set; }
        public bool DirtyWorkingTree { get; private set; }
        public DateTimeOffset BuildDate { get; private set; }
    }
}