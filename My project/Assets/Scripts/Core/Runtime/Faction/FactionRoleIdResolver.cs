namespace TwelveMoons.Core.Runtime
{
    public static class FactionRoleIdResolver
    {
        public const string NobleRoleId = "noble";
        public const string AcademyRoleId = "academy";
        public const string ChurchRoleId = "church";
        public const string CivilianRoleId = "civilian";

        public const string NobleConfigId = "F0004";
        public const string AcademyConfigId = "F0002";
        public const string ChurchConfigId = "F0003";
        public const string CivilianConfigId = "F0001";

        public static string ResolveConfiguredFactionId(FactionService factionService, string factionIdOrRoleId)
        {
            var normalized = (factionIdOrRoleId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(normalized))
            {
                return string.Empty;
            }

            if (IsConfigured(factionService, normalized))
            {
                return normalized;
            }

            var mappedId = GetConfiguredIdForRole(normalized);
            if (!string.IsNullOrEmpty(mappedId) && IsConfigured(factionService, mappedId))
            {
                return mappedId;
            }

            var legacyRoleId = GetRoleIdForConfiguredId(normalized);
            if (!string.IsNullOrEmpty(legacyRoleId) && IsConfigured(factionService, legacyRoleId))
            {
                return legacyRoleId;
            }

            return normalized;
        }

        public static string GetConfiguredIdForRole(string roleId)
        {
            switch ((roleId ?? string.Empty).Trim().ToLowerInvariant())
            {
                case NobleRoleId:
                    return NobleConfigId;
                case AcademyRoleId:
                    return AcademyConfigId;
                case ChurchRoleId:
                    return ChurchConfigId;
                case CivilianRoleId:
                    return CivilianConfigId;
                default:
                    return string.Empty;
            }
        }

        public static string GetRoleIdForConfiguredId(string configuredFactionId)
        {
            switch ((configuredFactionId ?? string.Empty).Trim())
            {
                case NobleConfigId:
                    return NobleRoleId;
                case AcademyConfigId:
                    return AcademyRoleId;
                case ChurchConfigId:
                    return ChurchRoleId;
                case CivilianConfigId:
                    return CivilianRoleId;
                default:
                    return string.Empty;
            }
        }

        private static bool IsConfigured(FactionService factionService, string factionId)
        {
            return factionService != null &&
                   !string.IsNullOrEmpty(factionId) &&
                   factionService.TryGetDefinition(factionId, out _);
        }
    }
}