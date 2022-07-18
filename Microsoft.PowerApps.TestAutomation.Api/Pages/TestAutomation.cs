// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestAutomation.Browser;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.PowerApps.TestAutomation.Api
{

    /// <summary>
    ///   Test Automation methods.
    ///  </summary>
    public class TestAutomation
        : AppPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestAutomation"/> class.
        /// </summary>
        /// <param name="browser">The browser.</param>
        public TestAutomation(InteractiveBrowser browser)
            : base(browser)
        {
        }

        public BrowserCommandResult<JObject> ExecuteTestAutomation(Uri uri, int testRunNumber, int maxWaitTimeInSeconds)
        {
            return this.Execute(GetOptions("Execute Test Automation"), driver =>
            {
                // Navigate to TestSuite or TestCase URL
                InitiateTest(driver, uri);

                // Check for existence of permissions dialog (1st test load for user)
                CheckForPermissionDialog(driver);

                // Try to report the sessionId. There is a bit of a race condition here,
                // so don't do this too close to fullscreen-app-host visibility or it 
                // will fail to find Core or some other namespace.
                TryReportSessionId(driver, testRunNumber);

                // Wait for test completion and collect results
                JObject testResults = WaitForTestResults(driver, maxWaitTimeInSeconds);

                return testResults;
            });
        }

        public BrowserCommandResult<List<Uri>> GetTestURLs(string filePath)
        {
            return this.Execute(GetOptions("Get List of Test URLs"), driver =>
            {
                //Replace encoded characters (%20) if present
                if (filePath.Contains("%20"))
                {
                    filePath = filePath.Replace("%20", " ");
                }
                // Initialize list of URLs
                List<Uri> testUrlList = new List<Uri>();

                // Read contents of json file
                JObject jObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(File.ReadAllText(filePath));

                // Retrieve list of Test URLs
                testUrlList = jObject["TestURLs"]?.ToObject<List<Uri>>();

                return testUrlList;
            });
        }

        public class TestSuiteResult
        {
            public string TestSuiteId { get; set; }
            public string TestSuiteName { get; set; }
            public string TestSuiteDescription { get; set; }
            public long StartTime { get; set; }
            public long EndTime { get; set; }
            public int TestsPassed { get; set; }
            public int TestsFailed { get; set; }
        }

        public class TestCaseResult
        {
            public string TestSuiteId { get; set; }
            public string TestSuiteName { get; set; }
            public string TestSuiteDescription { get; set; }
            public string TestCaseId { get; set; }
            public string TestCaseName { get; set; }
            public string TestCaseDescription { get; set; }
            public string TestFailureMessage { get; set; }
            public long StartTime { get; set; }
            public long EndTime { get; set; }
            public bool Success { get; set; }
            public ArraySegment<string> Traces { get; set; }

        }

        internal void CheckForPermissionDialog(IWebDriver driver)
        {
            // Switch to default content
            driver.SwitchTo().DefaultContent();

            var dialogButtons = driver.WaitUntilAvailable(By.XPath(Elements.Xpath[Reference.TestAutomation.PermissionDialogButtons]), new TimeSpan(0, 0, 5));

            if (dialogButtons != null)
            {
                // Should be two buttons (Allow, Don't Allow)
                var buttons = dialogButtons.FindElements(By.TagName("button"));

                foreach (var b in buttons)
                {
                    if (b.Text.Equals("Allow"))
                    {
                        b.Hover(driver, true);
                        b.Click(true);
                        b.SendKeys(Keys.Enter);
                        driver.WaitForPageToLoad();
                    }
                }
            }
        }

        internal JObject WaitForTestResults(IWebDriver driver, int maxWaitTimeInSeconds)
        {
            JObject jsonResultString = new JObject();
            jsonResultString = driver.WaitForTestResults(maxWaitTimeInSeconds);

            return jsonResultString;
        }

        internal void InitiateTest(IWebDriver driver, Uri uri)
        {
            driver.Navigate().GoToUrl(uri);

            // Wait for page to load
            driver.WaitForPageToLoad();

            // Wait for fullscreen-app-host
            driver.WaitUntilVisible(By.Id("fullscreen-app-host"));
            if (driver.IsVisible(By.Id("fullscreen-app-host")))
            {
                Debug.WriteLine("fullscreen-app-host is visible.");
            }
            else
            {
                Debug.WriteLine("fullscreen-app-host is not visible.");
            }
        }

        public Tuple<int, int> ReportResultsToDevOps(JObject jObject, int testRunNumber)
        {
            var testExecutionMode = (int)jObject.GetValue("ExecutionMode");

            int passCount = 0;
            int failCount = 0;


            if (testExecutionMode == 0)
            {
                // TestCase
                var testCaseResults = jObject["TestCaseResult"]?.ToObject<TestCaseResult>();

                if (testCaseResults.Success)
                {
                    passCount = 1;
                    failCount = 0;
                }
                else
                {
                    passCount = 0;
                    failCount = 1;

                }

                // Calculate Total Execution Time
                int testCaseElapsedMs = (int)(testCaseResults.EndTime - testCaseResults.StartTime);
                TimeSpan testCaseElapsedTime = new TimeSpan(0, 0, 0, 0, testCaseElapsedMs);

                var testCaseResult = testCaseResults.Success ? "Pass" : "Fail";

                // Output results to Console
                Console.WriteLine("\t" +
                    $"TestSuite Name: {testCaseResults.TestSuiteName} with ID {testCaseResults.TestSuiteId}");
                Console.WriteLine("\t" +
                    $"TestSuite Description: {testCaseResults.TestSuiteDescription}");
                Console.WriteLine("\t" +
                    $"TestCase Name: {testCaseResults.TestCaseName} with ID {testCaseResults.TestCaseId}");
                Console.WriteLine("\t" +
                    $"TestCase Description: {testCaseResults.TestCaseDescription}");
                Console.WriteLine("\t" +
                    $"Test Case Result: {testCaseResult}");
                Console.WriteLine("\t" +
                    $"Test Case Failure Message: {testCaseResults.TestFailureMessage}");
                Console.WriteLine("\t" +
                    $"Test Case execution time: {testCaseElapsedTime}");

            }
            else if (testExecutionMode == 1)
            {
                // Put JSON result objects into a TestSuiteResult
                var testSuiteResults = jObject["TestSuiteResult"]?.ToObject<TestSuiteResult>();

                var testSuiteCount = testSuiteResults.TestsPassed + testSuiteResults.TestsFailed;
                passCount = testSuiteResults.TestsPassed;
                failCount = testSuiteResults.TestsFailed;

                // Calculate Total Execution Time
                int testSuiteElapsedMs = (int)(testSuiteResults.EndTime - testSuiteResults.StartTime);
                TimeSpan testSuiteElapsedTime = new TimeSpan(0, 0, 0, 0, testSuiteElapsedMs);

                // Output results to Console
                Console.WriteLine("\t" +
                    $"TestSuite Name: {testSuiteResults.TestSuiteName} with ID {testSuiteResults.TestSuiteId}");
                Console.WriteLine("\t" +
                    $"TestSuite Description: {testSuiteResults.TestSuiteDescription}");
                Console.WriteLine("\t" +
                    $"Total Tests: {testSuiteCount}");
                Console.WriteLine("\t" +
                    $"Tests Passed: {testSuiteResults.TestsPassed}");
                Console.WriteLine("\t" +
                    $"Tests Failed: {testSuiteResults.TestsPassed}");
                Console.WriteLine("\t" +
                    $"TestSuite execution time: {testSuiteElapsedTime}");

            }

            var countPassFailResult = Tuple.Create(passCount, failCount);
            return countPassFailResult;
        }

        private void TryReportSessionId(IWebDriver driver, int testRunNumber)
        {
            string sessionId;
            try
            {
                sessionId = (string)driver.ExecuteScript("return Core.Telemetry.Log.sessionId");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception getting sessionId: {e.Message}");
                sessionId = null;
            }

            Debug.WriteLineIf(sessionId != null, $"Session ID for Test Run #{testRunNumber} is: {sessionId}");
            Debug.WriteLineIf(sessionId == null, $"Session ID for Test Run #{testRunNumber} is NULL");
        }
    }
}