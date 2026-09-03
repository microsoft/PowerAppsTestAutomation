// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ApiTestAutomation = Microsoft.PowerApps.TestAutomation.Api.TestAutomation;

namespace Microsoft.PowerApps.TestAutomation.Tests
{
    /// <summary>
    /// Unit tests for the console output helper. These do not drive a browser, and they
    /// deliberately carry no test category so that the PowerAppsTestAutomation category filter
    /// used by the UI test run does not select them.
    /// </summary>
    [TestClass]
    public class SanitizeForLogTests
    {
        [TestMethod]
        public void SanitizeForLog_ReturnsEmptyForNull()
        {
            Assert.AreEqual(string.Empty, ApiTestAutomation.SanitizeForLog(null));
        }

        [TestMethod]
        public void SanitizeForLog_ReturnsEmptyForEmpty()
        {
            Assert.AreEqual(string.Empty, ApiTestAutomation.SanitizeForLog(string.Empty));
        }

        [TestMethod]
        public void SanitizeForLog_LeavesOrdinaryTextUnchanged()
        {
            const string value = "Contoso Suite 1 - validates the order form (v2.1)";

            Assert.AreEqual(value, ApiTestAutomation.SanitizeForLog(value));
        }

        [TestMethod]
        public void SanitizeForLog_FlattensLineFeed()
        {
            Assert.AreEqual("before after", ApiTestAutomation.SanitizeForLog("before\nafter"));
        }

        [TestMethod]
        public void SanitizeForLog_FlattensCarriageReturn()
        {
            Assert.AreEqual("before after", ApiTestAutomation.SanitizeForLog("before\rafter"));
        }

        [TestMethod]
        public void SanitizeForLog_FlattensCarriageReturnLineFeed()
        {
            Assert.AreEqual("before  after", ApiTestAutomation.SanitizeForLog("before\r\nafter"));
        }

        [TestMethod]
        public void SanitizeForLog_FlattensUnicodeLineSeparators()
        {
            Assert.AreEqual("a b", ApiTestAutomation.SanitizeForLog("a\u0085b"));
            Assert.AreEqual("a b", ApiTestAutomation.SanitizeForLog("a\u2028b"));
            Assert.AreEqual("a b", ApiTestAutomation.SanitizeForLog("a\u2029b"));
        }

        [TestMethod]
        public void SanitizeForLog_ReplacesOtherControlCharacters()
        {
            Assert.AreEqual("a b", ApiTestAutomation.SanitizeForLog("a\tb"));
            Assert.AreEqual("a b", ApiTestAutomation.SanitizeForLog("a\0b"));
            Assert.AreEqual("a b", ApiTestAutomation.SanitizeForLog("a\u001bb"));
        }

        [TestMethod]
        public void SanitizeForLog_SeparatesAdjacentMarkerCharacters()
        {
            Assert.AreEqual("# #", ApiTestAutomation.SanitizeForLog("##"));
            Assert.AreEqual("# # #", ApiTestAutomation.SanitizeForLog("###"));
            Assert.AreEqual("# # # #", ApiTestAutomation.SanitizeForLog("####"));
        }

        [TestMethod]
        public void SanitizeForLog_KeepsSingleMarkerCharacter()
        {
            Assert.AreEqual("issue #42", ApiTestAutomation.SanitizeForLog("issue #42"));
        }

        [TestMethod]
        public void SanitizeForLog_ResultNeverContainsAdjacentMarkerCharacters()
        {
            string[] values =
            {
                "##", "###", "####", "a##b", "#\r#", "#\n#", "##vso[task.setvariable]",
            };

            foreach (string value in values)
            {
                string result = ApiTestAutomation.SanitizeForLog(value);

                Assert.IsFalse(
                    result.Contains("##"),
                    "Result for '" + value + "' still contains adjacent marker characters: " + result);
            }
        }

        [TestMethod]
        public void SanitizeForLog_ResultIsAlwaysASingleLine()
        {
            string[] values =
            {
                "one\ntwo",
                "one\r\ntwo",
                "one\rtwo",
                "one\u0085two",
                "one\u2028two",
                "one\u2029two",
                "trailing\n",
                "\nleading",
                "many\n\n\nbreaks",
            };

            foreach (string value in values)
            {
                string result = ApiTestAutomation.SanitizeForLog(value);

                Assert.AreEqual(
                    1,
                    result.Split(new[] { '\r', '\n', '\u0085', '\u2028', '\u2029' }).Length,
                    "Result for '" + value.Replace("\r", "\\r").Replace("\n", "\\n") + "' spans more than one line.");
            }
        }

        [TestMethod]
        public void SanitizeForLog_PreservesLength()
        {
            // Line terminators are replaced rather than removed, so no characters are lost.
            const string value = "a\nb\rc\td";

            Assert.AreEqual(value.Length, ApiTestAutomation.SanitizeForLog(value).Length);
        }
    }
}
