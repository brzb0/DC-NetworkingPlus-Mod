using UnityEngine;

namespace NetworkingPlus
{
    internal enum DeviceKind { Router, Firewall }

    internal sealed class DeviceDefinition
    {
        public string DisplayName;
        public float PriceMultiplier;
        public int XpToUnlock;
        public string ShopGuid;
        public Color DeviceColor;
        public DeviceKind Kind;
    }

    internal static class DeviceList
    {
        internal const int FixedPrice = 13500;

        internal static readonly DeviceDefinition[] All = new[]
        {
            new DeviceDefinition
            {
                DisplayName     = "Router QSFP+",
                PriceMultiplier = 1f,
                XpToUnlock      = 0,
                ShopGuid        = "netplus_router_qsfp_v1",
                DeviceColor     = new Color(0.1f, 0.7f, 0.2f, 1f),
                Kind            = DeviceKind.Router,
            },
            new DeviceDefinition
            {
                DisplayName     = "Firewall QSFP+",
                PriceMultiplier = 1f,
                XpToUnlock      = 0,
                ShopGuid        = "netplus_firewall_qsfp_v1",
                DeviceColor     = new Color(0.106f, 0.106f, 0.106f, 1f), // #1B1B1B
                Kind            = DeviceKind.Firewall,
            },
        };
    }
}
