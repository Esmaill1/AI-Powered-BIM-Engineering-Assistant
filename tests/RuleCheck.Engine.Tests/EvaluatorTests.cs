using RuleCheck.Core.Issues;
using RuleCheck.Core.Rules;
using RuleCheck.Core.Snapshot;
using static RuleCheck.Engine.Tests.TestBuilders;

namespace RuleCheck.Engine.Tests;

public sealed class RequiredParameterTests
{
    private readonly RuleEngine _engine = new();

    [Fact]
    public void Pass_When_Parameter_Present()
    {
        var snap = Snap(
            Elem(1, parameters: [("Mark", ParamValue.Text("W-001"))]));
        var report = _engine.Run(snap, Set(RequiredParamRule()));

        Assert.Empty(report.Issues);
        Assert.Equal(1, report.RulesEvaluated);
    }

    [Fact]
    public void Fail_When_Parameter_Missing()
    {
        var snap = Snap(Elem(1));
        var report = _engine.Run(snap, Set(RequiredParamRule()));

        Assert.Single(report.Issues);
        Assert.Contains("missing or empty", report.Issues[0].Message);
    }

    [Fact]
    public void Fail_When_Parameter_Empty_String()
    {
        var snap = Snap(
            Elem(1, parameters: [("Mark", ParamValue.Text(""))]));
        var report = _engine.Run(snap, Set(RequiredParamRule()));

        Assert.Single(report.Issues);
    }

    [Fact]
    public void Inherits_Severity_From_Rule()
    {
        var snap = Snap(Elem(1));
        var report = _engine.Run(snap, Set(RequiredParamRule(severity: Severity.Warning)));

        Assert.Single(report.Issues);
        Assert.Equal(Severity.Warning, report.Issues[0].Severity);
    }
}

public sealed class PatternTests
{
    private readonly RuleEngine _engine = new();

    [Fact]
    public void Pass_When_Value_Matches_Pattern()
    {
        var snap = Snap(
            Elem(1, parameters: [("Mark", ParamValue.Text("W-123"))]));
        var report = _engine.Run(snap, Set(PatternRule()));

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Fail_When_Value_Does_Not_Match()
    {
        var snap = Snap(
            Elem(1, parameters: [("Mark", ParamValue.Text("INVALID"))]));
        var report = _engine.Run(snap, Set(PatternRule()));

        Assert.Single(report.Issues);
        Assert.Contains("does not match pattern", report.Issues[0].Message);
    }

    [Theory]
    [InlineData("w-123")]  // lowercase should match (case-insensitive default)
    [InlineData("W-999")]
    public void Case_Insensitive_By_Default(string value)
    {
        var snap = Snap(
            Elem(1, parameters: [("Mark", ParamValue.Text(value))]));
        var report = _engine.Run(snap, Set(PatternRule()));

        Assert.Empty(report.Issues);
    }
}

public sealed class AllowedValuesTests
{
    private readonly RuleEngine _engine = new();

    [Fact]
    public void Pass_When_Value_In_Allowed_List()
    {
        var snap = Snap(
            Elem(1, parameters: [("Fire Rating", ParamValue.Text("2HR"))]));
        var report = _engine.Run(snap, Set(AllowedValuesRule()));

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Fail_When_Value_Not_In_List()
    {
        var snap = Snap(
            Elem(1, parameters: [("Fire Rating", ParamValue.Text("5HR"))]));
        var report = _engine.Run(snap, Set(AllowedValuesRule()));

        Assert.Single(report.Issues);
        Assert.Contains("not in the allowed list", report.Issues[0].Message);
    }

    [Fact]
    public void Case_Insensitive_Match()
    {
        var snap = Snap(
            Elem(1, parameters: [("Fire Rating", ParamValue.Text("2hr"))]));
        var report = _engine.Run(snap, Set(AllowedValuesRule()));

        Assert.Empty(report.Issues);
    }
}

public sealed class RangeTests
{
    private readonly RuleEngine _engine = new();

    [Fact]
    public void Pass_When_Value_In_Range()
    {
        var snap = Snap(
            Elem(1, parameters: [("Width", ParamValue.Number(5.0))]));
        var report = _engine.Run(snap, Set(RangeRule(min: 1.0, max: 10.0)));

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Fail_When_Below_Min()
    {
        var snap = Snap(
            Elem(1, parameters: [("Width", ParamValue.Number(0.5))]));
        var report = _engine.Run(snap, Set(RangeRule(min: 1.0)));

        Assert.Single(report.Issues);
        Assert.Contains("below minimum", report.Issues[0].Message);
    }

    [Fact]
    public void Fail_When_Above_Max()
    {
        var snap = Snap(
            Elem(1, parameters: [("Width", ParamValue.Number(15.0))]));
        var report = _engine.Run(snap, Set(RangeRule(max: 10.0)));

        Assert.Single(report.Issues);
        Assert.Contains("exceeds maximum", report.Issues[0].Message);
    }

    [Fact]
    public void Pass_At_Boundary_Within_Tolerance()
    {
        var snap = Snap(
            Elem(1, parameters: [("Width", ParamValue.Number(1.0 + 1e-9))]));
        var report = _engine.Run(snap, Set(RangeRule(min: 1.0, max: 10.0)));

        Assert.Empty(report.Issues);
    }
}

public sealed class UniqueTests
{
    private readonly RuleEngine _engine = new();

    [Fact]
    public void Pass_When_All_Values_Unique()
    {
        var snap = Snap(
            Elem(1, parameters: [("Mark", ParamValue.Text("W-001"))]),
            Elem(2, parameters: [("Mark", ParamValue.Text("W-002"))]));
        var report = _engine.Run(snap, Set(UniqueRule()));

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Fail_On_Duplicate_Values()
    {
        var snap = Snap(
            Elem(1, parameters: [("Mark", ParamValue.Text("W-001"))]),
            Elem(2, parameters: [("Mark", ParamValue.Text("W-001"))]),
            Elem(3, parameters: [("Mark", ParamValue.Text("W-002"))]));
        var report = _engine.Run(snap, Set(UniqueRule()));

        Assert.Equal(2, report.Issues.Count); // both duplicates flagged
    }

    [Fact]
    public void Empty_Values_Not_Flagged_As_Duplicates()
    {
        var snap = Snap(
            Elem(1, parameters: [("Mark", ParamValue.Text(""))]),
            Elem(2, parameters: [("Mark", ParamValue.Text(""))]));
        var report = _engine.Run(snap, Set(UniqueRule()));

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Scope_Model_Crosses_Categories()
    {
        var rule = UniqueRule(scope: UniqueScope.Model);
        var snap = Snap(
            Elem(1, category: "Walls", parameters: [("Mark", ParamValue.Text("X-001"))]),
            Elem(2, category: "Doors", parameters: [("Mark", ParamValue.Text("X-001"))]));
        // Both are "Walls" targeted, but only element 1 matches the category
        var report = _engine.Run(snap, Set(rule));

        Assert.Empty(report.Issues); // element 2 is category "Doors", not targeted
    }
}

public sealed class PlacementTests
{
    private readonly RuleEngine _engine = new();

    [Fact]
    public void Pass_When_Room_Assigned()
    {
        var snap = Snap(Elem(1, category: "Furniture", roomId: 100));
        var report = _engine.Run(snap, Set(PlacementRule()));

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Fail_When_No_Room()
    {
        var snap = Snap(Elem(1, category: "Furniture"));
        var report = _engine.Run(snap, Set(PlacementRule()));

        Assert.Single(report.Issues);
        Assert.Contains("not placed in any room", report.Issues[0].Message);
    }

    [Fact]
    public void Level_Check()
    {
        var snap = Snap(Elem(1, category: "Furniture"));
        var report = _engine.Run(snap, Set(PlacementRule(kind: PlacementKind.LevelAssigned)));

        Assert.Single(report.Issues);
        Assert.Contains("not assigned to any level", report.Issues[0].Message);
    }
}

public sealed class NotUnusedTests
{
    private readonly RuleEngine _engine = new();

    [Fact]
    public void Pass_When_Type_Is_Used()
    {
        var snap = Snap(
            Elem(100, isType: true),
            Elem(1, typeId: 100));  // instance uses type 100
        var report = _engine.Run(snap, Set(NotUnusedRule()));

        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Fail_When_Type_Is_Orphaned()
    {
        var snap = Snap(
            Elem(100, isType: true),
            Elem(1, typeId: 200));  // uses a different type
        var report = _engine.Run(snap, Set(NotUnusedRule()));

        Assert.Single(report.Issues);
        Assert.Contains("not used by any instance", report.Issues[0].Message);
    }
}

public sealed class RuleEngineOrchestrationTests
{
    private readonly RuleEngine _engine = new();

    [Fact]
    public void Skips_Not_Expressible_Rules()
    {
        var rule = new RuleDefinition
        {
            Id = "NE-001",
            Name = "Not Expressible",
            IsExpressible = false,
            UnexpressibleReason = "Needs visual check",
        };
        var snap = Snap(Elem(1));
        var report = _engine.Run(snap, Set(rule));

        Assert.Empty(report.Issues);
        Assert.Equal(0, report.RulesEvaluated);
        Assert.Equal(1, report.RulesSkipped);
    }

    [Fact]
    public void Report_Contains_Model_Title()
    {
        var snap = new ModelSnapshot { ModelTitle = "Golden Nugget" };
        var report = _engine.Run(snap, Set());

        Assert.Equal("Golden Nugget", report.ModelTitle);
    }

    [Fact]
    public void Category_Filter_Is_Case_Insensitive()
    {
        var rule = RequiredParamRule(category: "WALLS");
        var snap = Snap(Elem(1, category: "Walls")); // different case
        var report = _engine.Run(snap, Set(rule));

        Assert.Single(report.Issues); // element should still be matched
    }

    [Fact]
    public void Where_Conditions_Filter_Elements()
    {
        var rule = new RuleDefinition
        {
            Id = "WC-001",
            Name = "Where Condition Test",
            Target = new RuleTarget
            {
                Categories = ["Walls"],
                Where = [new WhereCondition
                {
                    Parameter = "Fire Rating",
                    Operator = ConditionOperator.Equals,
                    Value = RuleValue.Of("2HR"),
                }],
            },
            Check = new CheckSpec { Type = CheckType.RequiredParameter, Parameter = "Mark" },
        };
        var snap = Snap(
            Elem(1, parameters: [("Fire Rating", ParamValue.Text("2HR"))]),         // matches where, no Mark → issue
            Elem(2, parameters: [("Fire Rating", ParamValue.Text("1HR"))]));        // filtered out by where
        var report = _engine.Run(snap, Set(rule));

        Assert.Single(report.Issues);
        Assert.Equal(1L, report.Issues[0].ElementId);
    }

    [Fact]
    public void Multiple_Rules_Multiple_Elements()
    {
        var rules = Set(
            RequiredParamRule(id: "R1", parameter: "Mark"),
            RequiredParamRule(id: "R2", parameter: "Comments"));
        var snap = Snap(
            Elem(1, parameters: [("Mark", ParamValue.Text("W-001"))]),  // passes R1, fails R2
            Elem(2));                                                   // fails both
        var report = _engine.Run(snap, Set(rules.Rules.ToArray()));

        Assert.Equal(3, report.Issues.Count);
        Assert.Equal(2, report.RulesEvaluated);
    }
}
