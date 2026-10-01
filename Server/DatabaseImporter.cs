using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace TacticalDoorWedgeServer;

[Injectable]
public class DatabaseImporter(
    ISptLogger<DatabaseImporter> logger,
    DatabaseService databaseService,
    JsonUtil jsonUtil) : IOnLoad
{
    public const string ItemId = "tactical_door_wedge";
    public const string BaseCloneItemId = "59e366c186f7741778269d85"; // Piece of plexiglass
    public const string SkierId = "58330581ace78e27b88b4562";
    public const string NailsId = "59e3556486f7742d082403d5"; // Pack of nails
    public const string ToolsetId = "590c2e1186f7742459453e62"; // Toolset
    public const string MetalPartsId = "61bf7b6302b3924be92fa8c3"; // Metal spare parts
    public const string RublesId = "5449016a4bdc2d6f028b456f"; // Roubles

    public Task OnLoad()
    {
        try
        {
            var tables = databaseService.GetTables();
            if (tables == null) return Task.CompletedTask;

            // 1. Register Item Template
            if (tables.Templates?.Items != null && tables.Templates.Items.TryGetValue(BaseCloneItemId, out var baseItem) && baseItem != null)
            {
                string? json = jsonUtil.Serialize(baseItem);
                if (!string.IsNullOrEmpty(json))
                {
                    var newItem = jsonUtil.Deserialize<SPTarkov.Server.Core.Models.Eft.Common.Tables.TemplateItem>(json);
                    if (newItem != null)
                    {
                    newItem.Id = ItemId;
                    newItem.Name = "tactical_door_wedge";
                    if (newItem.Properties != null)
                    {
                        newItem.Properties.StackMaxSize = 2;
                        newItem.Properties.Weight = 0.25f;
                        newItem.Properties.Width = 1;
                        newItem.Properties.Height = 1;
                    }
                    tables.Templates.Items[ItemId] = newItem;
                    logger.Info("[TacticalDoorWedge] Registered item template: " + ItemId);
                }
            }
        }

            // 2. Register Handbook
            if (tables.Templates?.Handbook?.Items != null)
            {
                tables.Templates.Handbook.Items.Add(new SPTarkov.Server.Core.Models.Eft.Common.Tables.HandbookItem
                {
                    Id = ItemId,
                    ParentId = "5b59714086f77447477c2049", // Barter other
                    Price = 15000
                });
                logger.Info("[TacticalDoorWedge] Registered handbook price: 15,000 RUB");
            }

            // 3. Register Locales
            if (tables.Locales?.Global != null)
            {
                foreach (var (_, dict) in tables.Locales.Global)
                {
                    if (dict?.Value == null) continue;
                    dict.Value[$"{ItemId} Name"] = "Tactical Door Wedge";
                    dict.Value[$"{ItemId} ShortName"] = "Wedge";
                    dict.Value[$"{ItemId} Description"] = "A heavy-duty tactical polymer door wedge used to secure closed interior doors and prevent unauthorized entry.";
                }
                logger.Info("[TacticalDoorWedge] Registered localized names and descriptions.");
            }

            // 4. Register Skier Assort (LL3 Cash, LL2 Nails Barter)
            if (tables.Traders != null && tables.Traders.TryGetValue(SkierId, out var skier) && skier?.Assort != null)
            {
                var assort = skier.Assort;

                // Skier LL3 Cash purchase (15,000 RUB)
                string cashId = "wedge_cash_assort";
                assort.Items.Add(new SPTarkov.Server.Core.Models.Eft.Common.Tables.Item
                {
                    Id = cashId,
                    Template = ItemId,
                    ParentId = "hideout",
                    SlotId = "hideout",
                    Upd = new SPTarkov.Server.Core.Models.Eft.Common.Tables.Upd
                    {
                        StackObjectsCount = 999,
                        BuyRestrictionMax = 10,
                        BuyRestrictionCurrent = 0
                    }
                });

                assort.BarterScheme[cashId] =
                [
                    [
                        new SPTarkov.Server.Core.Models.Eft.Common.Tables.BarterScheme
                        {
                            Count = 15000,
                            Template = RublesId
                        }
                    ]
                ];
                assort.LoyalLevelItems[cashId] = 3;

                // Skier LL2 Barter (1x Pack of nails)
                string barterId = "wedge_barter_assort";
                assort.Items.Add(new SPTarkov.Server.Core.Models.Eft.Common.Tables.Item
                {
                    Id = barterId,
                    Template = ItemId,
                    ParentId = "hideout",
                    SlotId = "hideout",
                    Upd = new SPTarkov.Server.Core.Models.Eft.Common.Tables.Upd
                    {
                        StackObjectsCount = 999,
                        BuyRestrictionMax = 5,
                        BuyRestrictionCurrent = 0
                    }
                });

                assort.BarterScheme[barterId] =
                [
                    [
                        new SPTarkov.Server.Core.Models.Eft.Common.Tables.BarterScheme
                        {
                            Count = 1,
                            Template = NailsId
                        }
                    ]
                ];
                assort.LoyalLevelItems[barterId] = 2;

                logger.Info("[TacticalDoorWedge] Registered Skier assortments.");
            }

            // 5. Register Workbench Level 2 Craft
            var recipes = tables.Hideout?.Production?.Recipes;
            if (recipes != null)
            {
                var craft = new SPTarkov.Server.Core.Models.Eft.Hideout.HideoutProduction
                {
                    Id = "craft_tactical_door_wedge",
                    AreaType = (SPTarkov.Server.Core.Models.Enums.Hideout.HideoutAreas)10, // Workbench
                    Locked = false,
                    EndProduct = ItemId,
                    Count = 2,
                    ProductionTime = 1800, // 30 minutes
                    NeedFuelForAllProductionTime = false,
                    Continuous = false,
                    Requirements =
                    [
                        new SPTarkov.Server.Core.Models.Eft.Hideout.Requirement
                        {
                            Type = "Area",
                            AreaType = 10,
                            RequiredLevel = 2
                        },
                        new SPTarkov.Server.Core.Models.Eft.Hideout.Requirement
                        {
                            Type = "Tool",
                            TemplateId = ToolsetId
                        },
                        new SPTarkov.Server.Core.Models.Eft.Hideout.Requirement
                        {
                            Type = "Item",
                            TemplateId = BaseCloneItemId, // 1x Plexiglass
                            Count = 1,
                            IsFunctional = false,
                            IsEncoded = false
                        },
                        new SPTarkov.Server.Core.Models.Eft.Hideout.Requirement
                        {
                            Type = "Item",
                            TemplateId = MetalPartsId, // 1x Metal spare parts
                            Count = 1,
                            IsFunctional = false,
                            IsEncoded = false
                        }
                    ]
                };
                recipes.Add(craft);
                logger.Info("[TacticalDoorWedge] Registered Workbench Level 2 craft recipe.");
            }

            // 6. Register Static Loot
            if (tables.Locations != null)
            {
                try
                {
                    var locationProps = tables.Locations.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    int injectedCount = 0;

                    foreach (var prop in locationProps)
                    {
                        object? locObj = prop.GetValue(tables.Locations);
                        if (locObj == null) continue;

                        var staticLootProp = locObj.GetType().GetProperty("StaticLoot");
                        if (staticLootProp == null) continue;

                        object? staticLootObj = staticLootProp.GetValue(locObj);
                        if (staticLootObj is not System.Collections.IDictionary staticLootDict) continue;

                        foreach (System.Collections.DictionaryEntry entry in staticLootDict)
                        {
                            object? container = entry.Value;
                            if (container == null) continue;

                            var descProp = container.GetType().GetProperty("ContainerDescription");
                            var distProp = container.GetType().GetProperty("ItemDistribution");
                            if (descProp == null || distProp == null) continue;

                            object? desc = descProp.GetValue(container);
                            var idProp = desc?.GetType().GetProperty("Id");
                            string containerId = idProp?.GetValue(desc)?.ToString() ?? "";

                            if (containerId.Contains("578f8778245977358f744fe5") || // Toolbox
                                containerId.Contains("5d6d2b5486f774785c2ba8ea") || // Technical crate
                                containerId.Contains("578f8778245977358f744fe6"))   // Jacket
                            {
                                if (distProp.GetValue(container) is System.Collections.IList distList && distList.Count > 0)
                                {
                                    object sample = distList[0]!;
                                    string? distJson = jsonUtil.Serialize(sample);
                                    if (!string.IsNullOrEmpty(distJson))
                                    {
                                        object? newDist = jsonUtil.Deserialize(distJson, sample.GetType());
                                        if (newDist != null)
                                        {
                                            var tplProp = newDist.GetType().GetProperty("Tpl");
                                            var probProp = newDist.GetType().GetProperty("RelativeProbability");
                                            tplProp?.SetValue(newDist, ItemId);
                                            probProp?.SetValue(newDist, 100);
                                            distList.Add(newDist);
                                            injectedCount++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    logger.Info($"[TacticalDoorWedge] Injected wedge into {injectedCount} loot containers.");
                }
                catch (Exception ex)
                {
                    logger.Warning($"[TacticalDoorWedge] Loot injection fallback notice: {ex.Message}");
                }
            }

            logger.Info("[TacticalDoorWedge] Server mod loaded successfully: Item, shop, barters, craft, and loot registered.");
        }
        catch (Exception ex)
        {
            logger.Error($"[TacticalDoorWedge] Error during database import: {ex}");
        }

        return Task.CompletedTask;
    }
}
