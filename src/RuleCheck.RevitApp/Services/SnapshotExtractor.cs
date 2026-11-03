using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using RuleCheck.Core.Snapshot;

namespace RuleCheck.RevitApp.Services;

/// <summary>
/// Fast, read-only extraction of model elements, parameters, levels, and rooms
/// into a Revit-independent ModelSnapshot structure.
/// </summary>
public static class SnapshotExtractor
{
    private static readonly BuiltInCategory[] SupportedCategories =
    [
        BuiltInCategory.OST_Doors,
        BuiltInCategory.OST_Windows,
        BuiltInCategory.OST_Walls,
        BuiltInCategory.OST_Floors,
        BuiltInCategory.OST_Roofs,
        BuiltInCategory.OST_Columns,
        BuiltInCategory.OST_StructuralColumns,
        BuiltInCategory.OST_StructuralFraming,
        BuiltInCategory.OST_MechanicalEquipment,
        BuiltInCategory.OST_DuctTerminal,
        BuiltInCategory.OST_DuctCurves,
        BuiltInCategory.OST_PipeCurves,
        BuiltInCategory.OST_PlumbingFixtures,
        BuiltInCategory.OST_ElectricalEquipment,
        BuiltInCategory.OST_ElectricalFixtures
    ];

    public static ModelSnapshot Extract(Document doc)
    {
        var levels = ExtractLevels(doc);
        var rooms = ExtractRooms(doc);
        var elements = new List<ElementSnapshot>();

        var catFilter = new ElementMulticategoryFilter(SupportedCategories);

        // Extract instances
        var instances = new FilteredElementCollector(doc)
            .WherePasses(catFilter)
            .WhereElementIsNotElementType()
            .ToElements();

        var typeIds = new HashSet<ElementId>();

        foreach (var inst in instances)
        {
            var snap = ExtractElement(inst, isType: false);
            if (snap != null)
            {
                elements.Add(snap);
                var tid = inst.GetTypeId();
                if (tid != null && tid != ElementId.InvalidElementId)
                {
                    typeIds.Add(tid);
                }
            }
        }

        // Extract types referenced by those instances
        foreach (var tid in typeIds)
        {
            var typeElem = doc.GetElement(tid);
            if (typeElem != null)
            {
                var typeSnap = ExtractElement(typeElem, isType: true);
                if (typeSnap != null)
                {
                    elements.Add(typeSnap);
                }
            }
        }

        return new ModelSnapshot
        {
            ModelTitle = doc.Title,
            RevitVersion = doc.Application.VersionNumber,
            ExtractedAt = DateTimeOffset.UtcNow,
            Levels = levels,
            Rooms = rooms,
            Elements = elements
        };
    }

    private static IReadOnlyList<LevelInfo> ExtractLevels(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(Level))
            .Cast<Level>()
            .Select(lvl => new LevelInfo(lvl.Id.Value, lvl.Name, lvl.Elevation))
            .ToList();
    }

    private static IReadOnlyList<RoomInfo> ExtractRooms(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .Select(r => new RoomInfo(r.Id.Value, r.Name, r.Number, r.LevelId?.Value))
            .ToList();
    }

    private static ElementSnapshot? ExtractElement(Element elem, bool isType)
    {
        var cat = elem.Category;
        if (cat == null) return null;

        var catName = cat.BuiltInCategory.ToString();
        var paramDict = new Dictionary<string, ParamValue>(StringComparer.OrdinalIgnoreCase);

        foreach (Parameter param in elem.Parameters)
        {
            if (!param.HasValue) continue;
            var name = param.Definition.Name;
            if (string.IsNullOrWhiteSpace(name)) continue;

            var val = ConvertParamValue(param);
            if (!paramDict.ContainsKey(name))
            {
                paramDict[name] = val;
            }
        }

        long? levelId = null;
        if (!isType && elem.LevelId != null && elem.LevelId != ElementId.InvalidElementId)
        {
            levelId = elem.LevelId.Value;
        }

        long? roomId = null;
        if (!isType && elem is FamilyInstance fi)
        {
            var room = fi.Room ?? fi.ToRoom;
            if (room != null)
            {
                roomId = room.Id.Value;
            }
        }

        long? typeId = null;
        if (!isType)
        {
            var tid = elem.GetTypeId();
            if (tid != null && tid != ElementId.InvalidElementId)
            {
                typeId = tid.Value;
            }
        }

        return new ElementSnapshot
        {
            ElementId = elem.Id.Value,
            UniqueId = elem.UniqueId,
            Category = catName,
            IsType = isType,
            TypeId = typeId,
            LevelId = levelId,
            RoomId = roomId,
            Parameters = paramDict
        };
    }

    private static ParamValue ConvertParamValue(Parameter p)
    {
        return p.StorageType switch
        {
            StorageType.String => new ParamValue
            {
                Storage = StorageKind.String,
                HasValue = true,
                AsString = p.AsString(),
                Display = p.AsValueString() ?? p.AsString()
            },
            StorageType.Double => new ParamValue
            {
                Storage = StorageKind.Double,
                HasValue = true,
                AsDouble = p.AsDouble(),
                Display = p.AsValueString()
            },
            StorageType.Integer => new ParamValue
            {
                Storage = StorageKind.Integer,
                HasValue = true,
                AsInteger = p.AsInteger(),
                Display = p.AsValueString() ?? p.AsInteger().ToString()
            },
            StorageType.ElementId => new ParamValue
            {
                Storage = StorageKind.ElementId,
                HasValue = true,
                AsElementId = p.AsElementId().Value,
                Display = p.AsValueString()
            },
            _ => new ParamValue { Storage = StorageKind.None, HasValue = false }
        };
    }
}
