using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ModelContextProtocol.Protocol;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for <see cref="FormElicitationBuilder.From{T}"/> convention seeding, Exclude, and overrides.
    /// </summary>
    [TestClass]
    public class FormElicitationFromTypeTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        public void From_Build_WithNoChain_EmitsDefaults()
        {
            var built = FormElicitationBuilder.From<EmptyForm>("Confirm the action").Build();

            built.Mode.Should().Be("form");
            built.Message.Should().Be("Confirm the action");
            built.RequestedSchema!.Properties.Should().BeEmpty();
            built.RequestedSchema!.Required.Should().BeNull();
        }

        [TestMethod]
        public void From_KitchenSink_MapsEveryClrShape()
        {
            var built = FormElicitationBuilder.From<KitchenSinkForm>("Fill this out").Build();
            var properties = built.RequestedSchema!.Properties;

            properties["name"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Title.Should().Be("Name");
            properties["nickname"].Should().BeOfType<ElicitRequestParams.StringSchema>();
            properties["id"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().BeNull();
            properties["initial"].Should().BeOfType<ElicitRequestParams.StringSchema>();
            properties["homepage"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("uri");
            properties["birthday"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("date");
            properties["createdAt"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("date-time");
            properties["updatedAt"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("date-time");
            properties["active"].Should().BeOfType<ElicitRequestParams.BooleanSchema>();

            foreach (var key in new[] { "byteValue", "sByteValue", "shortValue", "uShortValue", "age", "uIntValue", "longValue", "uLongValue" })
            {
                properties[key].Should().BeOfType<ElicitRequestParams.NumberSchema>().Which.Type.Should().Be("integer");
            }

            foreach (var key in new[] { "floatValue", "doubleValue", "price" })
            {
                properties[key].Should().BeOfType<ElicitRequestParams.NumberSchema>().Which.Type.Should().Be("number");
            }

            var color = properties["color"].Should().BeOfType<ElicitRequestParams.TitledSingleSelectEnumSchema>().Subject;
            color.Title.Should().Be("Color");
            color.OneOf.Should().Contain(o => o.Const == "Red" && o.Title == "Red");
            color.OneOf.Should().Contain(o => o.Const == "BlueGreen" && o.Title == "Blue Green");

            var access = properties["access"].Should().BeOfType<ElicitRequestParams.TitledMultiSelectEnumSchema>().Subject;
            access.Items.AnyOf.Should().Contain(o => o.Const == "Read");
            access.Items.AnyOf.Should().Contain(o => o.Const == "Write");

            foreach (var key in new[] { "colors", "colorList", "colorSeq" })
            {
                properties[key].Should().BeOfType<ElicitRequestParams.TitledMultiSelectEnumSchema>();
            }

            built.RequestedSchema!.Required.Should().Contain("name");
            built.RequestedSchema!.Required.Should().NotContain("nickname");
            built.RequestedSchema!.Required.Should().Contain("age");
            built.RequestedSchema!.Required.Should().Contain("homepage");
        }

        [TestMethod]
        public void From_SkipsUnmappableProperties()
        {
            var built = FormElicitationBuilder.From<UnmappableForm>("m").Build();

            built.RequestedSchema!.Properties.Keys.Should().Equal("name");
        }

        [TestMethod]
        public void From_Exclude_RemovesSeededField()
        {
            var built = FormElicitationBuilder.From<UnmappableForm>("m")
                .Exclude(x => x.Name)
                .Build();

            built.RequestedSchema!.Properties.Should().BeEmpty();
        }

        [TestMethod]
        public void From_Exclude_Unmappable_IsSuccessfulNoOp()
        {
            var act = () => FormElicitationBuilder.From<UnmappableForm>("m")
                .Exclude(x => x.Home)
                .Exclude(x => x.Duration)
                .Build();

            var built = act.Should().NotThrow().Subject;
            built.RequestedSchema!.Properties.Keys.Should().Equal("name");
        }

        [TestMethod]
        public void From_Collision_ThrowsInvalidOperationException()
        {
            var act = () => FormElicitationBuilder.From<CollisionForm>("m");
            act.Should().Throw<InvalidOperationException>().WithMessage("*collide*");
        }

        [TestMethod]
        public void From_RequiredKeyword_MarksRequiredEvenWhenNullable()
        {
            var built = FormElicitationBuilder.From<RequiredKeywordForm>("m").Build();

            built.RequestedSchema!.Required.Should().Contain("code");
            built.RequestedSchema!.Required.Should().Contain("note");
            built.RequestedSchema!.Required.Should().NotContain("hint");
        }

        [TestMethod]
        public void From_NullableValueTypes_AreOptional()
        {
            var built = FormElicitationBuilder.From<NullablePrimitivesForm>("m").Build();

            built.RequestedSchema!.Required.Should().BeNull();
            built.RequestedSchema!.Properties["age"].Should().BeOfType<ElicitRequestParams.NumberSchema>()
                .Which.Type.Should().Be("integer");
            built.RequestedSchema!.Properties["flag"].Should().BeOfType<ElicitRequestParams.BooleanSchema>();
            built.RequestedSchema!.Properties["color"].Should().BeOfType<ElicitRequestParams.TitledSingleSelectEnumSchema>();
            built.RequestedSchema!.Properties["price"].Should().BeOfType<ElicitRequestParams.NumberSchema>()
                .Which.Type.Should().Be("number");
        }

        [TestMethod]
        public void From_SkipsIndexers()
        {
            var built = FormElicitationBuilder.From<IndexerForm>("m").Build();
            built.RequestedSchema!.Properties.Keys.Should().Equal("name");
        }

        [TestMethod]
        public void From_SkipsWriteOnlyAndStatic()
        {
            FormElicitationBuilder.From<WriteOnlyForm>("m").Build()
                .RequestedSchema!.Properties.Keys.Should().Equal("visible");
            FormElicitationBuilder.From<StaticPropertyForm>("m").Build()
                .RequestedSchema!.Properties.Keys.Should().Equal("name");
        }

        [TestMethod]
        public void From_IncludesInheritedProperties()
        {
            var built = FormElicitationBuilder.From<DerivedSignupForm>("m").Build();
            built.RequestedSchema!.Properties.Keys.Should().BeEquivalentTo("email", "name");
        }

        [TestMethod]
        public void From_EnumCollections_AllMultiChoice()
        {
            var built = FormElicitationBuilder.From<EnumCollectionsForm>("m").Build();
            foreach (var key in new[] { "array", "list", "sequence", "iList", "set" })
            {
                built.RequestedSchema!.Properties[key].Should().BeOfType<ElicitRequestParams.TitledMultiSelectEnumSchema>();
            }
        }

        [TestMethod]
        public void From_FluentOverride_RequiredAndTitle()
        {
            var built = FormElicitationBuilder.From<KitchenSinkForm>("m")
                .String(x => x.Name).Title("Full name").Optional()
                .Integer(x => x.Age).Min(18)
                .Build();

            var name = built.RequestedSchema!.Properties["name"].Should().BeOfType<ElicitRequestParams.StringSchema>().Subject;
            name.Title.Should().Be("Full name");
            built.RequestedSchema!.Required.Should().NotContain("name");

            built.RequestedSchema!.Properties["age"].Should().BeOfType<ElicitRequestParams.NumberSchema>()
                .Which.Minimum.Should().Be(18);
        }

        [TestMethod]
        public void From_AddsExtraStringNamedField()
        {
            var built = FormElicitationBuilder.From<EmptyForm>("m")
                .String("reason").Required()
                .Build();

            built.RequestedSchema!.Properties.Should().ContainKey("reason");
            built.RequestedSchema!.Required.Should().Equal("reason");
        }

        [TestMethod]
        public void From_ReconfigureSeededUriAsEmail()
        {
            var built = FormElicitationBuilder.From<KitchenSinkForm>("m")
                .Email(x => x.Homepage)
                .Build();

            built.RequestedSchema!.Properties["homepage"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Format.Should().Be("email");
        }

        [TestMethod]
        public void From_ExcludeThenReAdd()
        {
            var built = FormElicitationBuilder.From<KitchenSinkForm>("m")
                .Exclude(x => x.Name)
                .String(x => x.Name).Title("Brought back")
                .Build();

            built.RequestedSchema!.Properties["name"].Should().BeOfType<ElicitRequestParams.StringSchema>()
                .Which.Title.Should().Be("Brought back");
        }

        [TestMethod]
        public void From_TypedToInputRequest()
        {
            var request = FormElicitationBuilder.From<EmptyForm>("Confirm?").ToInputRequest();
            request.Method.Should().Be(RequestMethods.ElicitationCreate);
            request.ElicitationParams!.Message.Should().Be("Confirm?");
        }

        #endregion

    }

}
