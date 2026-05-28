using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NetworkingPlus
{
    // Helper component for cart button clicks (lambdas don't work with IL2CPP UnityAction)
    public class CartButtonHandler : MonoBehaviour
    {
        public CartButtonHandler(IntPtr ptr) : base(ptr) { }

        internal ShopCartItem cart;
        internal ComputerShop shop;
        internal bool isAdd;
        internal int itemID;
        internal PlayerManager.ObjectInHand itemType;

        private GameObject FindPrefab()
        {
            var mgm = MainGameManager.instance;
            if (mgm == null) return null;
            if (!DeviceRegistry.TryGet(itemID, out var entry)) return null;
            return Core.BuildDevicePrefab(mgm, itemID, entry);
        }

        private int FindFreeSpawnPoint()
        {
            var spawns = shop.transformProductItemsSpawns;
            if (spawns == null || spawns.Length == 0) return -1;
            for (int i = 0; i < spawns.Length; i++)
            {
                if (shop.itemsSpawnsInUse == null || i >= shop.itemsSpawnsInUse.Length || shop.itemsSpawnsInUse[i] == 0)
                    return i;
            }
            return -1;
        }

        private int SpawnAt(int spawnIndex)
        {
            var prefab = FindPrefab();
            if (prefab == null) return -1;
            var spawns = shop.transformProductItemsSpawns;
            var pos = spawns[spawnIndex];
            var obj = Object.Instantiate(prefab, pos.position, pos.rotation);
            Object.Destroy(prefab);
            if (shop.itemsSpawnsInUse != null && spawnIndex < shop.itemsSpawnsInUse.Length)
                shop.itemsSpawnsInUse[spawnIndex] = 1;
            int uid = shop.uniqueID++;
            if (shop.spawnedItems != null) shop.spawnedItems[uid] = obj;
            if (shop.spawnedItemPositions != null) shop.spawnedItemPositions[uid] = spawnIndex;
            return uid;
        }

        public void HandleClick()
        {
            if (cart == null || shop == null) return;
            if (cart.spawnedItemUIDs == null) return;

            if (isAdd)
            {
                if (cart.spawnedItemUIDs.Count >= 99) return;
                int spawnIdx = FindFreeSpawnPoint();
                if (spawnIdx < 0) return;
                int uid = SpawnAt(spawnIdx);
                if (uid < 0) return;
                cart.spawnedItemUIDs.Add(uid);
                cart.UpdateDisplay();
                shop.UpdateCartTotal();
            }
            else
            {
                if (cart.spawnedItemUIDs.Count <= 1)
                {
                    // Remove last spawned object
                    int lastUID = cart.spawnedItemUIDs[cart.spawnedItemUIDs.Count - 1];
                    if (shop.spawnedItems != null && shop.spawnedItems.ContainsKey(lastUID))
                    {
                        var obj = shop.spawnedItems[lastUID];
                        if (obj != null) Object.Destroy(obj);
                        shop.spawnedItems.Remove(lastUID);
                    }
                    if (shop.spawnedItemPositions != null && shop.spawnedItemPositions.ContainsKey(lastUID))
                    {
                        int posIdx = shop.spawnedItemPositions[lastUID];
                        if (shop.itemsSpawnsInUse != null && posIdx < shop.itemsSpawnsInUse.Length)
                            shop.itemsSpawnsInUse[posIdx] = 0;
                        shop.spawnedItemPositions.Remove(lastUID);
                    }
                    if (shop.cartUIItems != null)
                        shop.cartUIItems.Remove(cart);
                    shop.UpdateCartTotal();
                    Object.Destroy(cart.gameObject);
                    return;
                }
                // Remove last spawned object and decrement
                int lastUid = cart.spawnedItemUIDs[cart.spawnedItemUIDs.Count - 1];
                if (shop.spawnedItems != null && shop.spawnedItems.ContainsKey(lastUid))
                {
                    var obj = shop.spawnedItems[lastUid];
                    if (obj != null) Object.Destroy(obj);
                    shop.spawnedItems.Remove(lastUid);
                }
                if (shop.spawnedItemPositions != null && shop.spawnedItemPositions.ContainsKey(lastUid))
                {
                    int posIdx = shop.spawnedItemPositions[lastUid];
                    if (shop.itemsSpawnsInUse != null && posIdx < shop.itemsSpawnsInUse.Length)
                        shop.itemsSpawnsInUse[posIdx] = 0;
                    shop.spawnedItemPositions.Remove(lastUid);
                }
                cart.spawnedItemUIDs.RemoveAt(cart.spawnedItemUIDs.Count - 1);
                cart.UpdateDisplay();
                shop.UpdateCartTotal();
            }
        }
    }

    [HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.Awake))]
    internal static class PatchMainGameManagerAwake
    {
        private static void Postfix(MainGameManager __instance)
        {
            Core.SetupRegistry(__instance);
        }
    }

    [HarmonyPatch(typeof(MainGameManager), nameof(MainGameManager.Start))]
    internal static class PatchMainGameManagerStart
    {
        private static void Postfix(MainGameManager __instance)
        {
            if (DeviceRegistry.Entries.Count == 0)
                Core.SetupRegistry(__instance);
        }
    }

    [HarmonyPatch(typeof(ComputerShop), nameof(ComputerShop.SpawnPhysicalItem))]
    internal static class PatchSpawnPhysicalItem
    {
        private static bool Prefix(ref GameObject prefab, int price, PlayerManager.ObjectInHand itemType)
        {
            if (prefab == null) return true;
            var mgm = MainGameManager.instance;
            if (mgm == null) return true;

            var routers = mgm.routersPrefabs;
            if (routers != null)
            {
                for (int i = 0; i < routers.Length; i++)
                {
                    if (routers[i] == prefab)
                    {
                        int customID = Core.ROUTER_ID_BASE + i;
                        if (DeviceRegistry.TryGet(customID, out var e) && e.Kind == DeviceKind.Router)
                        {
                            var c = Core.BuildDevicePrefab(mgm, customID, e);
                            if (c != null) { prefab = c; return true; }
                        }
                        break;
                    }
                }
            }

            var firewalls = mgm.firewallsPrefabs;
            if (firewalls != null)
            {
                for (int i = 0; i < firewalls.Length; i++)
                {
                    if (firewalls[i] == prefab)
                    {
                        int customID = Core.FIREWALL_ID_BASE + i;
                        if (DeviceRegistry.TryGet(customID, out var e) && e.Kind == DeviceKind.Firewall)
                        {
                            var c = Core.BuildDevicePrefab(mgm, customID, e);
                            if (c != null) { prefab = c; return true; }
                        }
                        break;
                    }
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ComputerShop), nameof(ComputerShop.ButtonBuyShopItem))]
    internal static class PatchButtonBuyShopItem
    {
        private static bool Prefix(ComputerShop __instance, int itemID, int price,
                                   PlayerManager.ObjectInHand itemType, string displayName,
                                   bool isCustomColor)
        {
            if (!DeviceRegistry.TryGet(itemID, out var entry)) return true;

            var mgm = MainGameManager.instance;
            if (mgm == null) return true;

            var prefab = Core.BuildDevicePrefab(mgm, itemID, entry);
            if (prefab == null) return true;

            var spawns = __instance.transformProductItemsSpawns;
            if (spawns == null || spawns.Length == 0) { Object.Destroy(prefab); return false; }

            int idx = -1;
            for (int i = 0; i < spawns.Length; i++)
            {
                if (__instance.itemsSpawnsInUse == null || i >= __instance.itemsSpawnsInUse.Length || __instance.itemsSpawnsInUse[i] == 0)
                { idx = i; break; }
            }
            if (idx < 0) { Object.Destroy(prefab); return false; }

            var pos = spawns[idx];
            var obj = Object.Instantiate(prefab, pos.position, pos.rotation);
            Object.Destroy(prefab);

            if (__instance.itemsSpawnsInUse != null && idx < __instance.itemsSpawnsInUse.Length)
                __instance.itemsSpawnsInUse[idx] = 1;

            int uid = __instance.uniqueID++;
            if (__instance.spawnedItems != null) __instance.spawnedItems[uid] = obj;
            if (__instance.spawnedItemPositions != null) __instance.spawnedItemPositions[uid] = idx;

            // Cart — check if same item already in cart
            if (__instance.cartUIItems != null)
            {
                for (int i = 0; i < __instance.cartUIItems.Count; i++)
                {
                    var ci = __instance.cartUIItems[i];
                    if (ci != null && ci.itemID == itemID && ci.itemType == itemType)
                    {
                        ci.spawnedItemUIDs.Add(uid);
                        ci.UpdateDisplay();
                        __instance.UpdateCartTotal();
                        return false;
                    }
                }
            }

            // Create new cart item
            if (__instance.shopCartItemPrefab != null && __instance.parentForShopCartItems != null)
            {
                var cartGo = Object.Instantiate(__instance.shopCartItemPrefab, __instance.parentForShopCartItems);
                if (cartGo != null)
                {
                    var cart = cartGo.GetComponent<ShopCartItem>();
                    if (cart != null)
                    {
                        cart.shop = __instance;
                        cart.itemID = itemID;
                        cart.price = price;
                        cart.itemType = itemType;
                        cart.itemName = displayName;
                        cart.spawnedItemUIDs = new Il2CppSystem.Collections.Generic.List<int>();
                        cart.spawnedItemUIDs.Add(uid);

                        if (__instance.cartUIItems == null)
                            __instance.cartUIItems = new Il2CppSystem.Collections.Generic.List<ShopCartItem>();
                        __instance.cartUIItems.Add(cart);

                        // Wire up +/- buttons — each gets its own handler instance
                        if (cart.btnAdd != null)
                        {
                            cart.btnAdd.onClick.RemoveAllListeners();
                            var addHandler = cartGo.AddComponent<CartButtonHandler>();
                            addHandler.cart = cart;
                            addHandler.shop = __instance;
                            addHandler.isAdd = true;
                            addHandler.itemID = itemID;
                            addHandler.itemType = itemType;
                            cart.btnAdd.m_OnClick.AddListener(DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(addHandler.HandleClick));
                        }
                        if (cart.btnRemove != null)
                        {
                            cart.btnRemove.onClick.RemoveAllListeners();
                            var removeHandler = cartGo.AddComponent<CartButtonHandler>();
                            removeHandler.cart = cart;
                            removeHandler.shop = __instance;
                            removeHandler.isAdd = false;
                            removeHandler.itemID = itemID;
                            removeHandler.itemType = itemType;
                            cart.btnRemove.m_OnClick.AddListener(DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction>(removeHandler.HandleClick));
                        }

                        cart.UpdateDisplay();
                    }
                }
            }

            __instance.UpdateCartTotal();
            return false;
        }
    }

    [HarmonyPatch(typeof(ShopCartItem), nameof(ShopCartItem.UpdateDisplay))]
    internal static class PatchUpdateDisplay
    {
        private static void Postfix(ShopCartItem __instance)
        {
            if (!DeviceRegistry.TryGet(__instance.itemID, out var entry)) return;

            string label = entry.Kind == DeviceKind.Router ? "Router QSFP+" : "Firewall QSFP+";
            int qty = __instance.spawnedItemUIDs != null ? __instance.spawnedItemUIDs.Count : 1;

            if (__instance.txtItemName != null) __instance.txtItemName.text = label;
            if (__instance.txtAmount != null) __instance.txtAmount.text = qty.ToString();
            if (__instance.txtPrice != null) __instance.txtPrice.text = $"{__instance.price * qty} $";
        }
    }

    [HarmonyPatch(typeof(NetworkSwitch), nameof(NetworkSwitch.ButtonShowNetworkSwitchConfig))]
    internal static class PatchConfigButton
    {
        private static bool Prefix(NetworkSwitch __instance)
        {
            var mgm = MainGameManager.instance;
            if (mgm == null) return true;
            var usableObj = __instance.GetComponent<UsableObject>();
            if (usableObj == null) return true;
            if (!DeviceRegistry.TryGet(usableObj.prefabID, out var entry)) return true;

            if (entry.Kind == DeviceKind.Router)
            {
                var router = __instance.GetComponent<Router>();
                if (router != null) { mgm.ShowRouterConfigCanvas(router); return false; }
            }
            else if (entry.Kind == DeviceKind.Firewall)
            {
                var firewall = __instance.GetComponent<Firewall>();
                if (firewall != null) { mgm.ShowFirewallConfigCanvas(firewall); return false; }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(NetworkSwitch), nameof(NetworkSwitch.SwitchInsertedInRack))]
    internal static class PatchSwitchInsertedInRack
    {
        private static void Postfix(NetworkSwitch __instance, SwitchSaveData switchSaveData)
        {
            var usableObj = __instance.GetComponent<UsableObject>();
            if (usableObj == null) return;
            if (!DeviceRegistry.TryGet(usableObj.prefabID, out var entry)) return;

            string label = entry.Kind == DeviceKind.Router ? "Router QSFP+" : "Firewall QSFP+";
            __instance.switchId = label;
            if (__instance.txtScreen != null) __instance.txtScreen.text = label;
        }
    }
}
