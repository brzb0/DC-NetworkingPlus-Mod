using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[assembly: MelonInfo(typeof(NetworkingPlus.Core), "NetworkingPlus", "1.0.2", "Brzb02")]
[assembly: MelonGame("Waseku", "Data Center")]

namespace NetworkingPlus
{
    public class Core : MelonMod
    {
        internal static Sprite BaseSwitchSprite;
        internal static int BaseSwitchType = -1;
        internal static int BaseSwitchPrefabID = -1;

        internal const int ROUTER_ID_BASE  = 100;
        internal const int FIREWALL_ID_BASE = 200;

        internal static GameObject TemplateHolder { get; private set; }

        public override void OnInitializeMelon()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CartButtonHandler>();
        }

        internal static void SetupRegistry(MainGameManager mgm)
        {
            DeviceRegistry.Clear();

            var switchesPrefabs = mgm.switchesPrefabs;
            if (switchesPrefabs == null || switchesPrefabs.Length == 0) return;

            int bestIndex = -1;
            for (int i = 0; i < switchesPrefabs.Length; i++)
            {
                var go = switchesPrefabs[i];
                if (go == null) continue;
                string goName = go.name.ToLowerInvariant();
                if (goName.Contains("32") && goName.Contains("qsfp")) { bestIndex = i; break; }
            }
            if (bestIndex < 0 && switchesPrefabs.Length > 0) bestIndex = switchesPrefabs.Length - 1;
            if (bestIndex < 0) return;

            BaseSwitchType = bestIndex;
            BaseSwitchPrefabID = bestIndex;

            MelonLogger.Msg($"NetworkingPlus: Base switch = '{switchesPrefabs[bestIndex].name}'");

            // Template holder for inactive templates (needed for save/load)
            if (TemplateHolder != null) Object.Destroy(TemplateHolder);
            TemplateHolder = new GameObject("NetworkingPlus_TemplateHolder");
            TemplateHolder.SetActive(false);
            Object.DontDestroyOnLoad(TemplateHolder);

            // Extend arrays with custom device templates for save/load persistence
            int vanillaRouterCount = mgm.routersPrefabs?.Length ?? 0;
            int routerSlots = ROUTER_ID_BASE + CountKind(DeviceKind.Router);
            var extendedRouters = new GameObject[routerSlots];
            for (int i = 0; i < vanillaRouterCount; i++)
                extendedRouters[i] = mgm.routersPrefabs[i];

            int vanillaFirewallCount = mgm.firewallsPrefabs?.Length ?? 0;
            int firewallSlots = FIREWALL_ID_BASE + CountKind(DeviceKind.Firewall);
            var extendedFirewalls = new GameObject[firewallSlots];
            for (int i = 0; i < vanillaFirewallCount; i++)
                extendedFirewalls[i] = mgm.firewallsPrefabs[i];

            int nextRouterID = ROUTER_ID_BASE;
            int nextFirewallID = FIREWALL_ID_BASE;

            foreach (var def in DeviceList.All)
            {
                int id = def.Kind == DeviceKind.Router ? nextRouterID++ : nextFirewallID++;

                DeviceRegistry.Register(id, new DeviceRegistry.Entry(
                    BaseSwitchPrefabID, id, def.Kind, BaseSwitchType, def.PriceMultiplier
                ));

                // Build inactive template for save/load reference
                var template = BuildDevicePrefab(mgm, id, new DeviceRegistry.Entry(
                    BaseSwitchPrefabID, id, def.Kind, BaseSwitchType, def.PriceMultiplier
                ), TemplateHolder.transform);
                if (template != null) template.name = $"Device_template_{id}";

                if (def.Kind == DeviceKind.Router)
                    extendedRouters[id] = template;
                else
                    extendedFirewalls[id] = template;

                MelonLogger.Msg($"NetworkingPlus: Registered '{def.DisplayName}' (id={id})");
            }

            mgm.routersPrefabs = extendedRouters;
            mgm.firewallsPrefabs = extendedFirewalls;
        }

        private static int CountKind(DeviceKind kind)
        {
            int c = 0;
            foreach (var d in DeviceList.All) if (d.Kind == kind) c++;
            return c;
        }

        internal static GameObject BuildDevicePrefab(MainGameManager mgm, int prefabID,
                                                     DeviceRegistry.Entry entry,
                                                     Transform parent = null)
        {
            var basePrefab = mgm.switchesPrefabs[entry.BaseSwitchPrefabID];
            if (basePrefab == null) return null;

            var clone = parent != null
                ? Object.Instantiate(basePrefab, parent, false)
                : Object.Instantiate(basePrefab);
            clone.name = $"Device_custom_{prefabID}";

            var netSwitch = clone.GetComponent<NetworkSwitch>();
            if (netSwitch != null)
            {
                netSwitch.switchType = prefabID; // Use custom ID so save/load finds the right template
                netSwitch.switchId = entry.Kind == DeviceKind.Router ? "Router QSFP+" : "Firewall QSFP+";
            }

            if (entry.Kind == DeviceKind.Router)
            {
                var router = clone.AddComponent<Router>();
                router.routingTable = new Il2CppSystem.Collections.Generic.List<Router.SubnetRoute>();
                router.asn = prefabID;
                router.switchType = prefabID; // Use custom ID
                router.switchId = "Router QSFP+";
            }
            else
            {
                var firewall = clone.AddComponent<Firewall>();
                firewall.filterRules = new Il2CppSystem.Collections.Generic.List<Firewall.FilterRule>();
                firewall.clusterIP = "";
                firewall.switchType = prefabID; // Use custom ID
                firewall.switchId = "Firewall QSFP+";
            }

            var usableObj = clone.GetComponent<UsableObject>();
            if (usableObj != null) usableObj.prefabID = prefabID;

            ApplyDeviceTint(clone, prefabID);
            return clone;
        }

        internal static void ApplyDeviceTint(GameObject root, int prefabID)
        {
            if (root == null) return;

            int defIndex = -1;
            if (prefabID >= ROUTER_ID_BASE)
            {
                int localIdx = prefabID - ROUTER_ID_BASE;
                int count = 0;
                for (int i = 0; i < DeviceList.All.Length; i++)
                {
                    if (DeviceList.All[i].Kind == DeviceKind.Router)
                    {
                        if (count == localIdx) { defIndex = i; break; }
                        count++;
                    }
                }
            }
            else if (prefabID >= FIREWALL_ID_BASE)
            {
                int localIdx = prefabID - FIREWALL_ID_BASE;
                int count = 0;
                for (int i = 0; i < DeviceList.All.Length; i++)
                {
                    if (DeviceList.All[i].Kind == DeviceKind.Firewall)
                    {
                        if (count == localIdx) { defIndex = i; break; }
                        count++;
                    }
                }
            }
            if (defIndex < 0 || defIndex >= DeviceList.All.Length) return;
            Color tint = DeviceList.All[defIndex].DeviceColor;

            string[] colorProps = { "_Color", "_BaseColor", "_MainColor", "_TintColor", "_Tint", "_AlbedoColor" };

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                if (rend == null) continue;
                var mats = rend.materials;
                bool changed = false;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null) continue;
                    foreach (var prop in colorProps)
                        if (mats[m].HasProperty(prop)) { mats[m].SetColor(prop, tint); changed = true; }
                }
                if (changed) { rend.materials = mats; break; }
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (buildIndex != 0) MelonCoroutines.Start(AddShopItems());
        }

        private IEnumerator AddShopItems()
        {
            yield return new WaitForSeconds(1.5f);

            var mgm = MainGameManager.instance;
            if (mgm == null) yield break;
            var computerShop = mgm.computerShop;
            if (computerShop == null) yield break;

            ShopItem sourceItem = null;
            if (computerShop.shopItems != null)
            {
                foreach (var si in computerShop.shopItems)
                {
                    if (si == null || si.shopItemSO == null) continue;
                    if ((int)si.shopItemSO.itemType == 4 && si.shopItemSO.itemID == BaseSwitchPrefabID)
                    { sourceItem = si; BaseSwitchSprite = si.shopItemSO.sprite; }
                }
            }
            if (sourceItem == null) yield break;

            var shopParent = computerShop.shopItemParent;
            if (shopParent == null) yield break;
            var modsTransform = shopParent.transform.Find("HL Mods");
            if (modsTransform != null) shopParent = modsTransform.gameObject;

            float itemHeight = 0f;
            var sourceRt = sourceItem.GetComponent<RectTransform>();
            if (sourceRt != null) itemHeight = sourceRt.rect.height;

            int addedCount = 0;
            foreach (var def in DeviceList.All)
            {
                int prefabID = def.Kind == DeviceKind.Router
                    ? ROUTER_ID_BASE + IndexOfKind(DeviceKind.Router, def)
                    : FIREWALL_ID_BASE + IndexOfKind(DeviceKind.Firewall, def);
                if (!DeviceRegistry.TryGet(prefabID, out _)) continue;

                var added = AddShopButton(sourceItem, shopParent, prefabID, def.Kind,
                    def.DisplayName, DeviceList.FixedPrice, def.XpToUnlock, def.ShopGuid);
                if (added != null) addedCount++;
            }

            var containerRt = shopParent.GetComponent<RectTransform>();
            if (containerRt != null && itemHeight > 0f && addedCount > 0)
            {
                var sd = containerRt.sizeDelta;
                sd.y += (itemHeight + 20f) * addedCount + 100f;
                containerRt.sizeDelta = sd;
                var scrollContent = shopParent.transform.parent;
                if (scrollContent != null)
                {
                    var contentRt = scrollContent.GetComponent<RectTransform>();
                    if (contentRt != null)
                    {
                        var csd = contentRt.sizeDelta;
                        csd.y += (itemHeight + 20f) * addedCount + 100f;
                        contentRt.sizeDelta = csd;
                    }
                }
            }
            Canvas.ForceUpdateCanvases();
        }

        private static int IndexOfKind(DeviceKind kind, DeviceDefinition target)
        {
            int idx = 0;
            foreach (var def in DeviceList.All)
            { if (def.Kind == kind) { if (def == target) return idx; idx++; } }
            return 0;
        }

        private static GameObject AddShopButton(ShopItem source, GameObject parent, int prefabID,
                                               DeviceKind kind, string label, int price,
                                               int xpToUnlock, string guid)
        {
            var newSO = ScriptableObject.CreateInstance<ShopItemSO>();
            newSO.itemName = label;
            newSO.price = price;
            newSO.xpToUnlock = xpToUnlock;
            newSO.itemType = kind == DeviceKind.Router
                ? PlayerManager.ObjectInHand.Router
                : PlayerManager.ObjectInHand.Firewall;
            newSO.itemID = prefabID;
            newSO.eol = source.shopItemSO.eol;
            newSO.sprite = BaseSwitchSprite;

            var cloned = Object.Instantiate(source.gameObject, parent.transform, false);
            cloned.name = $"ShopItem_{label.Replace(" ", "_")}";
            cloned.transform.localPosition = Vector3.zero;
            cloned.transform.localScale = Vector3.one;

            var shopItem = cloned.GetComponent<ShopItem>();
            if (shopItem == null) { Object.Destroy(cloned); return null; }

            shopItem.shopItemSO = newSO;
            shopItem.guid = guid;

            if (shopItem.txtName != null) shopItem.txtName.text = label;
            if (shopItem.txtPrice != null) shopItem.txtPrice.text = $"${price:N0}";
            if (shopItem.itemDisplayName != null) shopItem.itemDisplayName = label;

            var btnExt = cloned.GetComponent<ButtonExtended>();
            if (btnExt != null)
            {
                btnExt.doSubmitOnSelect = false;
                btnExt.selectOnPointerEnter = false;
                btnExt.functionToBeCalledOnSelect.RemoveAllListeners();
            }

            cloned.SetActive(true);
            return cloned;
        }
    }
}
