using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace RustMaps;

[HarmonyPatch(typeof(ServerMgr), nameof(ServerMgr.OpenConnection))]
public static class SaddleVendorFix
{
	private const string StablesShopKeeperPrefab = "assets/prefabs/npc/bandit/shopkeepers/stables_shopkeeper.prefab";
	
	private const string InvisibleVendingMachinePrefab = "assets/prefabs/deployable/vendingmachine/npcvendingmachines/shopkeeper_vm_invis.prefab";
	
	[HarmonyPrefix]
	private static void Prefix()
	{
		FixSaddleVendors();
	}

	private static void FixSaddleVendors()
	{
		foreach (var shopKeeper in BaseNetworkable.serverEntities.OfType<NPCShopKeeper>())
		{
			if (shopKeeper is null || 
			    shopKeeper.invisibleVendingMachineRef.IsValid(true) || 
			    shopKeeper.PrefabName != StablesShopKeeperPrefab)
			{
#if DEBUG
				Debug.Log("[SaddleVendorFix] Shop keeper found but does not match the necessary criteria.");
#endif
				continue;
			}

			var transform = shopKeeper.transform;
			if (GameManager.server.CreateEntity(
				    InvisibleVendingMachinePrefab, 
				    transform.position + new Vector3(0f, -1.5f, 0f) + transform.forward * 1.5f, 
				    transform.rotation,
				    false) 
			    is not InvisibleVendingMachine invisibleVendingMachine)
			{
				Debug.LogError($"[SaddleVendorFix] Unable to create the vending machine for the shop keeper at {transform.position.ToString()}. Please contact plugin developer(s).");
				continue;
			}
			
			Debug.Log($"[SaddleVendorFix] Found a saddle vendor at {transform.position.ToString()} with a missing invisible vending machine. Attempting to fix...");
			
			invisibleVendingMachine.Spawn();
			invisibleVendingMachine.EnableSaving(false);
			
			invisibleVendingMachine.SetFlag(VendingMachine.VendingMachineFlags.EmptyInv, true);
			invisibleVendingMachine.SetFlag(VendingMachine.VendingMachineFlags.Broadcasting, true);
			invisibleVendingMachine.UpdateMapMarker();
			
			invisibleVendingMachine.vendingOrders = invisibleVendingMachine.vmoManifest.GetFromIndex(18);
			invisibleVendingMachine.InstallFromVendingOrders();
			invisibleVendingMachine.SendNetworkUpdateImmediate();
			
			// Special thanks to lencorp on Discord for helping get this part working again after a Rust update. 
			invisibleVendingMachine.SetAttachedNPC(shopKeeper);
			shopKeeper.invisibleVendingMachineRef.Set(invisibleVendingMachine);
			
			Debug.Log("[SaddleVendorFix] Fix applied!");
		}
	}
}