using CosmoWhisper.Managers;
using Xunit;

namespace CosmoWhisper.Tests
{
    public class TextProcessorTests
    {
        [Fact]
        public void CleanText_StripsLeadingPunctuationAndTrailingHallucinations()
        {
            string cleaned = TextProcessor.CleanText("... Hello world, thanks for watching.");
            Assert.Equal("Hello world", cleaned);
        }

        [Fact]
        public void CleanText_ConvertsParagraphAndLineCommands_WhileProtectingOneLine()
        {
            Assert.Equal("First\n\nSecond", TextProcessor.CleanText("First new paragraph Second"));
            Assert.Equal("First line\vSecond line", TextProcessor.CleanText("First line new line Second line"));
            Assert.Equal("Keep one line intact", TextProcessor.CleanText("Keep one line intact"));
            Assert.Equal("\v", TextProcessor.CleanText("line"));
            Assert.Equal("\n\n", TextProcessor.CleanText("paragraph"));
        }

        [Fact]
        public void CleanText_RemovesDisfluenciesAndAppliesPunctuationCommands()
        {
            string result = TextProcessor.CleanText("Hi (coughs) comma how are you today period");
            Assert.Equal("Hi , how are you today .", result);
        }

        [Fact]
        public void CleanText_ShortSegmentPreservesQuestionAndExclamationButStripsTrailingPeriod()
        {
            Assert.Equal("Yes?", TextProcessor.CleanText("Yes?"));
            Assert.Equal("Great!", TextProcessor.CleanText("Great!"));
            Assert.Equal("Okay", TextProcessor.CleanText("Okay."));
        }

        [Fact]
        public void NormalizeSpokenNumbersAndEmails_NormalizesEmailsAndLongDigitSequences()
        {
            Assert.Equal(
                "Contact louis@gmail.com or 07802",
                TextProcessor.NormalizeSpokenNumbersAndEmails("Contact louis at gmail dot com or zero seven eight zero two"));

            Assert.Equal("Wait one two seconds", TextProcessor.NormalizeSpokenNumbersAndEmails("Wait one two seconds"));
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("wwww", true)]
        [InlineData("Thank you for watching.", true)]
        [InlineData("bye bye bye bye", true)]
        [InlineData("\n\n", false)]
        [InlineData("\v", false)]
        [InlineData("Hi", false)]
        [InlineData("4", false)]
        public void IsGarbage_IdentifiesHallucinationsWhileKeepingValidShortTextAndFormatting(string input, bool expected)
        {
            Assert.Equal(expected, TextProcessor.IsGarbage(input));
        }
    }
}
