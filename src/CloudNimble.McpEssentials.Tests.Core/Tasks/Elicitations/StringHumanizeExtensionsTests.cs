using CloudNimble.Breakdance.Extensions.MSTest2;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CloudNimble.McpEssentials.Tests.Core.Tasks.Elicitations
{

    /// <summary>
    /// Coverage for the vendored Humanizer PascalCase splitter used as form-label defaults.
    /// Happy-path cases match Humanizer v2.14.1 <c>StringHumanizeTests</c>. The rest try to
    /// break the splitter: punctuation, empty tokens, mixed separators, Unicode, and null.
    /// </summary>
    [TestClass]
    public class StringHumanizeExtensionsTests : BreakdanceMSTestBase
    {

        #region Public Methods

        [TestMethod]
        [DataRow("PascalCaseInputStringIsTurnedIntoSentence", "Pascal case input string is turned into sentence")]
        [DataRow("WhenIUseAnInputAHere", "When I use an input a here")]
        [DataRow("10IsInTheBegining", "10 is in the begining")]
        [DataRow("NumberIsFollowedByLowerCase5th", "Number is followed by lower case 5th")]
        [DataRow("NumberIsAtTheEnd100", "Number is at the end 100")]
        [DataRow("XIsFirstWordInTheSentence", "X is first word in the sentence")]
        [DataRow("XIsFirstWordInTheSentence ThenThereIsASpace", "X is first word in the sentence then there is a space")]
        [DataRow("ContainsSpecial?)@Characters", "Contains special characters")]
        [DataRow("a", "A")]
        [DataRow("A", "A")]
        [DataRow("?)@", "")]
        [DataRow("?", "")]
        [DataRow("", "")]
        [DataRow("JeNeParlePasFrançais", "Je ne parle pas français")]
        [DataRow("HTML", "HTML")]
        [DataRow("TheHTMLLanguage", "The HTML language")]
        [DataRow("HTMLIsTheLanguage", "HTML is the language")]
        [DataRow("TheLanguage IsHTML", "The language is HTML")]
        [DataRow("TheLanguageIsHTML", "The language is HTML")]
        [DataRow("HTML5", "HTML 5")]
        [DataRow("1HTML", "1 HTML")]
        [DataRow("camelCaseText", "Camel case text")]
        [DataRow("HTMLParser", "HTML parser")]
        [DataRow("XMLHTTPRequest", "XMLHTTP request")]
        [DataRow("IOStream", "IO stream")]
        [DataRow("AB CD", "Ab cd")]
        public void Humanize_PascalCaseAndAcronyms_MatchHumanizerV214(string input, string expected)
        {
            input.Humanize().Should().Be(expected);
        }

        [TestMethod]
        [DataRow("Underscored_input_string_is_turned_into_sentence", "Underscored input string is turned into sentence")]
        [DataRow("Underscored_input_String_is_turned_INTO_sentence", "Underscored input String is turned INTO sentence")]
        [DataRow("dash-separated-string", "dash separated string")]
        [DataRow("foo_Bar-Baz", "foo Bar Baz")]
        [DataRow("TEST 1 - THIS IS A TEST", "TEST 1 THIS IS A TEST")]
        [DataRow("TEST 1 -THIS IS A TEST", "TEST 1 THIS IS A TEST")]
        [DataRow("TEST 1- THIS IS A TEST", "TEST 1 THIS IS A TEST")]
        [DataRow("TEST 1_ THIS IS A TEST", "TEST 1 THIS IS A TEST")]
        [DataRow("TEST 1 _THIS IS A TEST", "TEST 1 THIS IS A TEST")]
        [DataRow("TEST 1 _ THIS IS A TEST", "TEST 1 THIS IS A TEST")]
        [DataRow("TEST 1 - THIS_IS_A_TEST", "TEST 1 THIS IS A TEST")]
        [DataRow("TEST 1 - THIS is A Test", "TEST 1 THIS is A test")]
        public void Humanize_Separators_MatchHumanizerV214(string input, string expected)
        {
            input.Humanize().Should().Be(expected);
        }

        [TestMethod]
        public void Humanize_Null_ThrowsArgumentNullException()
        {
            string value = null!;
            var act = () => value.Humanize();
            act.Should().Throw<ArgumentNullException>().WithParameterName("input");
        }

        [TestMethod]
        public void Humanize_WhitespaceOnly_ReturnsEmpty()
        {
            "   ".Humanize().Should().BeEmpty();
        }

        [TestMethod]
        public void Humanize_OnlyUnderscores_ProducesEmptyTokensJoinedAsSpaces()
        {
            "___".Humanize().Should().Be("   ");
        }

        [TestMethod]
        public void Humanize_OnlyDashes_ProducesEmptyTokensJoinedAsSpaces()
        {
            "---".Humanize().Should().Be("   ");
        }

        [TestMethod]
        public void Humanize_LeadingAndTrailingUnderscores_KeepEmptyEdgeTokens()
        {
            "_name_".Humanize().Should().Be(" name ");
        }

        [TestMethod]
        public void Humanize_ArabicLetters_PassThrough()
        {
            "جمهورية ألمانيا الاتحادية".Humanize().Should().Be("جمهورية ألمانيا الاتحادية");
        }

        [TestMethod]
        public void Humanize_UsesInvariantCasing_NotTurkishI()
        {
            "istanbul".Humanize().Should().Be("Istanbul");
        }

        [TestMethod]
        [DataRow("FirstName", "First Name")]
        [DataRow("EmailAddress", "Email Address")]
        [DataRow("emailAddress", "Email Address")]
        [DataRow("HTMLParser", "HTML Parser")]
        [DataRow("HTML", "HTML")]
        [DataRow("CanReturnTitleCase", "Can Return Title Case")]
        [DataRow("Title_humanization_Honors_ALLCAPS", "Title Humanization Honors ALLCAPS")]
        [DataRow("a", "A")]
        [DataRow("A", "A")]
        [DataRow("", "")]
        [DataRow("?)@", "")]
        [DataRow("HTML5", "HTML 5")]
        [DataRow("TimeToLive", "Time To Live")]
        public void Titleize_Identifier_TitleCasesEachWord(string input, string expected)
        {
            input.Titleize().Should().Be(expected);
        }

        [TestMethod]
        public void Titleize_Null_ThrowsArgumentNullException()
        {
            string value = null!;
            var act = () => value.Titleize();
            act.Should().Throw<ArgumentNullException>().WithParameterName("input");
        }

        [TestMethod]
        public void Titleize_FreestandingDash_SkipsEmptyWordsFromSplit()
        {
            var titled = "foo - bar".Titleize();
            titled.Should().NotContain("  ");
            titled.Should().Be("Foo Bar");
        }

        [TestMethod]
        public void Titleize_DoesNotLowerEnglishSmallWords_UnlikeHumanizerTitleCase()
        {
            "TimeToLive".Titleize().Should().Be("Time To Live");
        }

        #endregion

    }

}
