using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class LetterDefinition
    {
        public LetterDefinition(ConfigRow row)
        {
            LetterId = row.GetString("LetterId");
            Title = row.GetString("Title");
            SenderName = row.GetString("SenderName");
            BodyText = row.GetString("BodyText");
            Remark = row.GetString("Remark");
        }

        public string LetterId { get; }

        public string Title { get; }

        public string SenderName { get; }

        public string BodyText { get; }

        public string Remark { get; }
    }
}
