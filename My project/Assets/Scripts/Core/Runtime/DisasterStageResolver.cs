using System.Collections.Generic;
using System.Linq;
using TwelveMoons.Core.Config;

namespace TwelveMoons.Core.Runtime
{
    public sealed class DisasterStageResolver
    {
        private readonly List<DisasterStageDefinition> stages = new List<DisasterStageDefinition>();

        public DisasterStageResolver(ConfigTable table)
        {
            if (table == null)
            {
                return;
            }

            foreach (var row in table.Rows)
            {
                var stage = new DisasterStageDefinition(row);
                if (!string.IsNullOrEmpty(stage.StageId))
                {
                    stages.Add(stage);
                }
            }
        }

        public IReadOnlyList<DisasterStageDefinition> Stages => stages;

        public DisasterStageDefinition Resolve(string disasterId, int round)
        {
            return stages
                .Where(stage => stage.Contains(disasterId, round))
                .OrderBy(stage => stage.StartRound)
                .ThenBy(stage => stage.EndRound)
                .FirstOrDefault();
        }
    }
}
