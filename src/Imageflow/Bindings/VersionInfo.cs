using System.Text.Json.Nodes;

namespace Imageflow.Bindings
{
    public class VersionInfo
    {
        private VersionInfo(JsonNode versionInfo)
        {
            // LongVersionString = versionInfo.long_version_string.Value;
            // LastGitCommit = versionInfo.last_git_commit.Value;
            // DirtyWorkingTree = versionInfo.dirty_working_tree.Value;
            // GitTag = versionInfo.git_tag?.Value;
            // GitDescribeAlways = versionInfo.git_describe_always?.Value;
            // // Sometimes Newtonsoft gives us a DateTime; other times a string.
            // object dateTime = versionInfo.build_date.Value;
            // if (dateTime is string time)
            // {
            //     BuildDate = XmlConvert.ToDateTime(time, XmlDateTimeSerializationMode.Utc);
            // }
            // else if (dateTime is DateTime dt)
            // {
            //     BuildDate = new DateTimeOffset(dt);
            // }
            
            var obj = versionInfo.AsObject();
            const string longVersionMsg = "Imageflow get_version_info responded with null version_info.long_version_string";
            LongVersionString = obj.TryGetPropertyValue("long_version_string", out var longVersionValue)
                ? longVersionValue?.GetValue<string>() ?? throw new ImageflowAssertionFailed(longVersionMsg)
                : throw new ImageflowAssertionFailed(longVersionMsg);
            const string lastGitCommitMsg = "Imageflow get_version_info responded with null version_info.last_git_commit";
            LastGitCommit = obj.TryGetPropertyValue("last_git_commit", out var lastGitCommitValue)
                ? lastGitCommitValue?.GetValue<string>() ?? throw new ImageflowAssertionFailed(lastGitCommitMsg)
                : throw new ImageflowAssertionFailed(lastGitCommitMsg);
            const string dirtyWorkingTreeMsg = "Imageflow get_version_info responded with null version_info.dirty_working_tree";
            DirtyWorkingTree = obj.TryGetPropertyValue("dirty_working_tree", out var dirtyWorkingTreeValue)
                ? dirtyWorkingTreeValue?.GetValue<bool>() ?? throw new ImageflowAssertionFailed(dirtyWorkingTreeMsg)
                : throw new ImageflowAssertionFailed(dirtyWorkingTreeMsg);
            const string buildDateMsg = "Imageflow get_version_info responded with null version_info.build_date";
            BuildDate = obj.TryGetPropertyValue("build_date", out var buildDateValue)
                ? buildDateValue?.GetValue<DateTimeOffset>() ?? throw new ImageflowAssertionFailed(buildDateMsg)
                : throw new ImageflowAssertionFailed(buildDateMsg);
            // git tag and git describe are optional
            GitTag = obj.TryGetPropertyValue("git_tag", out var gitTagValue)
                ? gitTagValue?.GetValue<string>()
                : null;
            GitDescribeAlways = obj.TryGetPropertyValue("git_describe_always", out var gitDescribeAlwaysValue)
                ? gitDescribeAlwaysValue?.GetValue<string>()
                : null;
            
            
            
        }
        internal static VersionInfo FromNode(JsonNode versionInfo)
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