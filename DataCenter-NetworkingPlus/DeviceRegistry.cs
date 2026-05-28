using System.Collections.Generic;

namespace NetworkingPlus
{
    internal static class DeviceRegistry
    {
        internal readonly struct Entry
        {
            internal readonly int BaseSwitchPrefabID;
            internal readonly int CustomPrefabID;
            internal readonly DeviceKind Kind;
            internal readonly int SwitchType;
            internal readonly float PriceMultiplier;

            internal Entry(int baseSwitchPrefabID, int customPrefabID, DeviceKind kind,
                           int switchType, float priceMultiplier)
            {
                BaseSwitchPrefabID = baseSwitchPrefabID;
                CustomPrefabID     = customPrefabID;
                Kind               = kind;
                SwitchType         = switchType;
                PriceMultiplier    = priceMultiplier;
            }
        }

        private static readonly Dictionary<int, Entry> _entries = new();

        internal static IReadOnlyDictionary<int, Entry> Entries => _entries;

        internal static void Register(int prefabID, Entry entry) => _entries[prefabID] = entry;

        internal static bool TryGet(int prefabID, out Entry entry) =>
            _entries.TryGetValue(prefabID, out entry);

        internal static void Clear() => _entries.Clear();
    }
}
