﻿using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace R2API {

    namespace ItemDropAPITools {

        public class DropList {
            /*
                The purpose of this class is to store the original drop lists for a run and to generate and store the master drop lists for a run.
                As well as changing and reverting the drop lists during a run.
                Instead of drop lists being created from scratch, the master drop lists are generated by modifying a vanilla drop list.
                Therefore items can be added to or removed from the original list.

                This is why I wanted a way to remove an item from both the add and remove lists, so the original behavior could be restored,
                    without first having to calculated whether an item would originally be apart the drop list.
                Becuse otherwise to restore original behavior, if the item was originally apart of the drop list, adding it to the add drop list,
                    would not change the master drop list.
                If it was not originally part of the drop list, adding it to the remove drop list would not change the master drop list.
                But you would have to know what it was going to do oringally to restore its original behavior.
                Being able to remove the item from both lists I thought would be the easier way to restore original behavior, but I was told having a function for this was confusing.

                The original lists are saved because both the player drops api and monster drop api will want apply alterations to the original lists.
                Rather than whichever api is executed second using the first api's altered lists as a base.
            */

            public static bool OriginalListsSaved;
            public static List<ItemIndex> AvailableItemsOriginal;
            public static List<EquipmentIndex> AvailableEquipmentOriginal;
            /* Single-tier item drop lists */
            public static List<PickupIndex> Tier1DropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> Tier2DropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> Tier3DropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> BossDropListOriginal = new List<PickupIndex>();          // RoR2 only puts items, not equipment, in the availableBossDropList
            public static List<PickupIndex> LunarItemDropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> VoidTier1DropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> VoidTier2DropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> VoidTier3DropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> VoidBossDropListOriginal = new List<PickupIndex>();
            /* Single-tier equipment drop lists */
            public static List<PickupIndex> EquipmentDropListOriginal = new List<PickupIndex>();
            public static List<PickupIndex> LunarEquipmentDropListOriginal = new List<PickupIndex>();
            /* Combined drop lists */
            public static List<PickupIndex> LunarDropListOriginal = new List<PickupIndex>();
            /* Special drops */
            public static List<PickupIndex> SpecialItemsOriginal = new List<PickupIndex>();
            public static List<PickupIndex> SpecialEquipmentOriginal = new List<PickupIndex>();
            


            /* Backups for SetDropLists/RevertDropLists */
            private static List<PickupIndex> Tier1DropListBackup = new List<PickupIndex>();
            private static List<PickupIndex> Tier2DropListBackup = new List<PickupIndex>();
            private static List<PickupIndex> Tier3DropListBackup = new List<PickupIndex>();
            private static List<PickupIndex> EquipmentDropListBackup = new List<PickupIndex>();


            /* Single-tier item drop lists */
            public List<PickupIndex> AvailableTier1DropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableTier2DropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableTier3DropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableBossDropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableLunarItemDropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableVoidTier1DropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableVoidTier2DropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableVoidTier3DropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableVoidBossDropList = new List<PickupIndex>();
            /* Single tier equipment drop lists */
            public List<PickupIndex> AvailableEquipmentDropList = new List<PickupIndex>();
            public List<PickupIndex> AvailableLunarEquipmentDropList = new List<PickupIndex>();
            /* Combined drop lists */
            public List<PickupIndex> AvailableLunarDropList = new List<PickupIndex>();
            /* Special drops */
            public List<PickupIndex> AvailableSpecialItems = new List<PickupIndex>();
            public List<PickupIndex> AvailableSpecialEquipment = new List<PickupIndex>();

            [Obsolete("Use AvailableEquipmentDropList instead")]
            public List<PickupIndex> AvailableNormalEquipmentDropList {
                get { return AvailableEquipmentDropList; }
                set { AvailableEquipmentDropList = value; }
            }



            public UnityEngine.Events.UnityEvent ListsGenerated = new UnityEngine.Events.UnityEvent();


            public List<PickupIndex> GetDropList(ItemTier itemTier) {
                if (itemTier == ItemTier.Tier1) {
                    return AvailableTier1DropList;
                }
                else if (itemTier == ItemTier.Tier2) {
                    return AvailableTier2DropList;
                }
                else if (itemTier == ItemTier.Tier3) {
                    return AvailableTier3DropList;
                }
                else if (itemTier == ItemTier.Boss) {
                    return AvailableBossDropList;
                }
                else if (itemTier == ItemTier.Lunar) {
                    return AvailableLunarItemDropList;
                }
                else if (itemTier == ItemTier.VoidTier1) {
                    return AvailableVoidTier1DropList;
                }
                else if (itemTier == ItemTier.VoidTier2) {
                    return AvailableVoidTier2DropList;
                }
                else if (itemTier == ItemTier.VoidTier3) {
                    return AvailableVoidTier3DropList;
                }
                else if (itemTier == ItemTier.VoidBoss) {
                    return AvailableVoidBossDropList;
                }
                else {
                    return AvailableEquipmentDropList;
                }
            }

            public static void OnRunStart(On.RoR2.Run.orig_Start orig, Run run) {
                OriginalListsSaved = false;
                orig(run);
            }

            /*
                Creates a new list containing all the items in another list.
                Returns a list containing none when given an empty list to fix a bug caused by having a list with a length of zero.
            */
            public static List<PickupIndex> BackupDropList(IEnumerable<PickupIndex> list) {
                return list.ToList().ToList();
            }

            //  Clears all the drop lists in the Run class.
            public void ClearAllLists(Run run) {
                run.availableItems.Clear();
                run.availableEquipment.Clear();
                run.availableTier1DropList.Clear();
                run.availableTier2DropList.Clear();
                run.availableTier3DropList.Clear();
                run.availableBossDropList.Clear();
                run.availableLunarItemDropList.Clear();
                run.availableVoidTier1DropList.Clear();
                run.availableVoidTier2DropList.Clear();
                run.availableVoidTier3DropList.Clear();
                run.availableVoidBossDropList.Clear();
                run.availableEquipmentDropList.Clear();
                run.availableLunarEquipmentDropList.Clear();
                run.availableLunarCombinedDropList.Clear();
            }

            //  Backs up all the original drop lists generated for this run.
            public void DuplicateDropLists(Run run) {
                if (!OriginalListsSaved) {
                    AvailableItemsOriginal = run.availableItems.ToList();
                    AvailableEquipmentOriginal = run.availableEquipment.ToList();
                    Tier1DropListOriginal = BackupDropList(run.availableTier1DropList);
                    Tier2DropListOriginal = BackupDropList(run.availableTier2DropList);
                    Tier3DropListOriginal = BackupDropList(run.availableTier3DropList);
                    BossDropListOriginal = BackupDropList(run.availableBossDropList);
                    LunarItemDropListOriginal = BackupDropList(run.availableLunarItemDropList);
                    VoidTier1DropListOriginal = BackupDropList(run.availableVoidTier1DropList);
                    VoidTier2DropListOriginal = BackupDropList(run.availableVoidTier2DropList);
                    VoidTier3DropListOriginal = BackupDropList(run.availableVoidTier3DropList);
                    VoidBossDropListOriginal = BackupDropList(run.availableVoidBossDropList);
                    EquipmentDropListOriginal = BackupDropList(run.availableEquipmentDropList);
                    LunarEquipmentDropListOriginal = BackupDropList(run.availableLunarEquipmentDropList);
                    LunarDropListOriginal = BackupDropList(run.availableLunarCombinedDropList);

                    SpecialItemsOriginal.Clear();
                    foreach (var itemIndex in Catalog.SpecialItems) {
                        if (run.availableItems.Contains(itemIndex)) {
                            SpecialItemsOriginal.Add(PickupCatalog.FindPickupIndex(itemIndex));
                        }
                    }
                    foreach (var itemIndex in Catalog.ScrapItems.Values) {
                        if (run.availableItems.Contains(itemIndex)) {
                            SpecialItemsOriginal.Add(PickupCatalog.FindPickupIndex(itemIndex));
                        }
                    }

                    SpecialEquipmentOriginal.Clear();
                    foreach (var equipmentIndex in Catalog.EliteEquipment) {
                        var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                        if (equipmentDef.unlockableDef == null || PreGameController.AnyUserHasUnlockable(equipmentDef.unlockableDef)) {
                            SpecialEquipmentOriginal.Add(PickupCatalog.FindPickupIndex(equipmentIndex));
                        }
                    }

                    OriginalListsSaved = true;
                }
            }

            //  Saves the adjusted drop lists, which are the master lists.
            internal void GenerateDropLists(
                List<PickupIndex> pickupsToAdd,
                List<PickupIndex> pickupsToRemove,
                List<PickupIndex> pickupsSpecialToAdd,
                List<PickupIndex> pickupsSpecialToRemove) {

                AvailableTier1DropList = BackupDropList(Tier1DropListOriginal);
                AvailableTier2DropList = BackupDropList(Tier2DropListOriginal);
                AvailableTier3DropList = BackupDropList(Tier3DropListOriginal);
                AvailableBossDropList = BackupDropList(BossDropListOriginal);
                AvailableLunarItemDropList = BackupDropList(LunarItemDropListOriginal);
                AvailableVoidTier1DropList = BackupDropList(VoidTier1DropListOriginal);
                AvailableVoidTier2DropList = BackupDropList(VoidTier2DropListOriginal);
                AvailableVoidTier3DropList = BackupDropList(VoidTier3DropListOriginal);
                AvailableVoidBossDropList = BackupDropList(VoidBossDropListOriginal);

                AvailableSpecialItems = BackupDropList(SpecialItemsOriginal);
                AvailableEquipmentDropList = BackupDropList(EquipmentDropListOriginal);
                AvailableLunarEquipmentDropList = BackupDropList(LunarEquipmentDropListOriginal);
                AvailableSpecialEquipment = BackupDropList(SpecialEquipmentOriginal);

                List<List<PickupIndex>> alterations = new List<List<PickupIndex>>() { pickupsToAdd, pickupsToRemove, pickupsSpecialToAdd, pickupsSpecialToRemove };
                for (int listIndex = 0; listIndex < alterations.Count; listIndex++) {
                    List<PickupIndex> currentList = alterations[listIndex];
                    bool special = listIndex > 1;
                    foreach (PickupIndex pickupIndex in currentList) {
                        List<PickupIndex> dropList = new List<PickupIndex>();
                        ItemIndex itemIndex = PickupCatalog.GetPickupDef(pickupIndex).itemIndex;
                        EquipmentIndex equipmentIndex = PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex;
                        if (itemIndex != ItemIndex.None) {
                            ItemTier itemTier = ItemCatalog.GetItemDef(itemIndex).tier;
                            if (!special) {
                                if (itemTier == ItemTier.Tier1) {
                                    dropList = AvailableTier1DropList;
                                }
                                else if (itemTier == ItemTier.Tier2) {
                                    dropList = AvailableTier2DropList;
                                }
                                else if (itemTier == ItemTier.Tier3) {
                                    dropList = AvailableTier3DropList;
                                }
                                else if (itemTier == ItemTier.Boss) {
                                    dropList = AvailableBossDropList;
                                }
                                else if (itemTier == ItemTier.Lunar) {
                                    dropList = AvailableLunarItemDropList;
                                }
                                else if (itemTier == ItemTier.VoidTier1) {
                                    dropList = AvailableVoidTier1DropList;
                                }
                                else if (itemTier == ItemTier.VoidTier2) {
                                    dropList = AvailableVoidTier2DropList;
                                }
                                else if (itemTier == ItemTier.VoidTier3) {
                                    dropList = AvailableVoidTier3DropList;
                                }
                                else if (itemTier == ItemTier.VoidBoss) {
                                    dropList = AvailableVoidBossDropList;
                                }
                            }
                            else {
                                dropList = AvailableSpecialItems;
                            }
                        }
                        else if (equipmentIndex != EquipmentIndex.None) {
                            EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                            if (!special) {
                                if (equipmentDef.isLunar) {
                                    dropList = AvailableLunarEquipmentDropList;
                                }
                                else if (equipmentDef.isBoss) {
                                    dropList = AvailableBossDropList;
                                }
                                else {
                                    dropList = AvailableEquipmentDropList;
                                }
                            }
                            else {
                                dropList = AvailableSpecialEquipment;
                            }
                        }

                        if (listIndex % 2 == 0) {
                            if (dropList.Contains(pickupIndex) == false) {
                                dropList.Add(pickupIndex);
                            }
                        }
                        else {
                            if (dropList.Contains(pickupIndex)) {
                                dropList.Remove(pickupIndex);
                            }
                        }
                    }
                }

                AvailableLunarDropList.Clear();
                AvailableLunarDropList.AddRange(AvailableLunarItemDropList);
                AvailableLunarDropList.AddRange(AvailableLunarEquipmentDropList);

                ListsGenerated.Invoke();
            }

            // Add drops to the Run drop lists and enable corresponding availableItems/equipment
            // Supports adding any item/equipment to any droplist
            internal void AddDrops(Run run, List<PickupIndex> dropList, IEnumerable<PickupIndex> pickupsToAdd) {
                foreach (var pickupIndex in pickupsToAdd) {
                    if (dropList != null) {
                        dropList.Add(pickupIndex);
                    }
                    ItemIndex itemIndex = PickupCatalog.GetPickupDef(pickupIndex).itemIndex;
                    EquipmentIndex equipmentIndex = PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex;
                    if (itemIndex != ItemIndex.None) {
                        run.availableItems.Add(itemIndex);
                    }
                    else if (equipmentIndex != EquipmentIndex.None) {
                        run.availableEquipment.Add(equipmentIndex);
                    }
                }
            }

            //  Sets the drop lists in Run using the adjusted, master lists.
            public void SetItems(Run run) {
                AddDrops(run, run.availableTier1DropList, AvailableTier1DropList);
                AddDrops(run, run.availableTier2DropList, AvailableTier2DropList);
                AddDrops(run, run.availableTier3DropList, AvailableTier3DropList);
                AddDrops(run, run.availableBossDropList, AvailableBossDropList);
                AddDrops(run, run.availableLunarItemDropList, AvailableLunarItemDropList);
                AddDrops(run, run.availableVoidTier1DropList, AvailableVoidTier1DropList);
                AddDrops(run, run.availableVoidTier2DropList, AvailableVoidTier2DropList);
                AddDrops(run, run.availableVoidTier3DropList, AvailableVoidTier3DropList);
                AddDrops(run, run.availableVoidBossDropList, AvailableVoidBossDropList);
                AddDrops(run, run.availableEquipmentDropList, AvailableEquipmentDropList);
                AddDrops(run, run.availableLunarEquipmentDropList, AvailableLunarEquipmentDropList);

                AddDrops(run, run.availableLunarCombinedDropList, AvailableLunarDropList);

                // There's no drop list to add special items or equipment to, only enable them
                AddDrops(run, null, AvailableSpecialItems);
                AddDrops(run, null, AvailableSpecialEquipment);
            }

            public static List<PickupIndex> ToPickupIndices(IEnumerable<ItemIndex> indices) {
                return indices.Select(PickupCatalog.FindPickupIndex).ToList();
            }

            public static List<PickupIndex> ToPickupIndices(IEnumerable<EquipmentIndex> indices) {
                return indices.Select(PickupCatalog.FindPickupIndex).ToList();
            }

            /*
                This will backup the four main drop lists in Run and then overwrite them.
                The intention is this function is used when changing the drop lists only temporarily.
            */
            public static void SetDropLists(IEnumerable<PickupIndex> givenTier1, IEnumerable<PickupIndex> givenTier2,
                IEnumerable<PickupIndex> givenTier3, IEnumerable<PickupIndex> givenEquipment) {

                var run = Run.instance;
                Tier1DropListBackup = BackupDropList(run.availableTier1DropList);
                Tier2DropListBackup = BackupDropList(run.availableTier2DropList);
                Tier3DropListBackup = BackupDropList(run.availableTier3DropList);
                EquipmentDropListBackup = BackupDropList(run.availableEquipmentDropList);

                run.availableTier1DropList = BackupDropList(givenTier1);
                run.availableTier2DropList = BackupDropList(givenTier2);
                run.availableTier3DropList = BackupDropList(givenTier3);
                run.availableEquipmentDropList = BackupDropList(givenEquipment);
            }

            //  This function will revert the four main drop lists in Run to how they were before they were changed temporarily.
            public static void RevertDropLists() {
                var run = Run.instance;
                run.availableTier1DropList = BackupDropList(Tier1DropListBackup);
                run.availableTier2DropList = BackupDropList(Tier2DropListBackup);
                run.availableTier3DropList = BackupDropList(Tier3DropListBackup);
                run.availableEquipmentDropList = BackupDropList(EquipmentDropListBackup);
            }
        }
    }
}
