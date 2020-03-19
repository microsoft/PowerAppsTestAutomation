// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestAutomation.Browser;
using Microsoft.PowerApps.TestAutomation.Api;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.PowerApps.TestAutomation.Tests
{
    [TestClass]
    public class TestAutomation
    {
        private static string _username = "";
        private static string _password = "";
        private static BrowserType _browserType;
        private static Uri _xrmUri;
        private static Uri _testAutomationUri;
        private static string _resultsDirectory = "";
        private static string _driversPath = "";
        private static string _usePrivateMode;
        private static string _testAutomationURLFilePath = "";
        private static int _globalTestCount = 0;
        private static int _globalPassCount = 0;
        private static int _globalFailCount = 0;
        private static int _testMaxWaitTimeInSeconds = 600;

        public TestContext TestContext { get; set; }

        private static TestContext _testContext;

        [ClassInitialize]
        public static void Initialize(TestContext TestContext)
        {
            _testContext = TestContext;

            _username = _testContext.Properties["OnlineUsername"].ToString();
            _password = _testContext.Properties["OnlinePassword"].ToString();
            _xrmUri = new Uri(_testContext.Properties["OnlineUrl"].ToString());
            _resultsDirectory = _testContext.Properties["ResultsDirectory"].ToString();
            _browserType = (BrowserType)Enum.Parse(typeof(BrowserType), _testContext.Properties["BrowserType"].ToString());
            _driversPath = _testContext.Properties["DriversPath"].ToString();
            _usePrivateMode = _testContext.Properties["UsePrivateMode"].ToString();
            _testAutomationURLFilePath = _testContext.Properties["TestAutomationURLFilePath"].ToString();
            _testMaxWaitTimeInSeconds = Convert.ToInt16(_testContext.Properties["TestMaxWaitTimeInSeconds"]);
        }

        [TestCategory("PowerAppsTestAutomation")]
        [Priority(1)]
        [TestMethod]
        public void RunTestAutomation()
        {
            BrowserOptions options = RunTestSettings.Options;
            options.BrowserType = _browserType;
            options.DriversPath = _driversPath;

            using (var appBrowser = new PowerAppBrowser(options))
            {
                // Track current test iteration
                int testRunCounter = 0;
                // Track list of  Test Automation URLs
                var testUrlList = appBrowser.TestAutomation.GetTestURLs(_testAutomationURLFilePath);
                // Track total number of TestURLs
                int testUrlCount = testUrlList.Value.Count();

                foreach (Uri testUrl in testUrlList?.Value)
                {
                    // Test URL
                    _testAutomationUri = testUrl;
                    testRunCounter += 1;

                    try
                    {
                        // if TestCounter > 1, authentication not required
                        if (testRunCounter <= 1)
                        {
                            //Login To PowerApps
                            Debug.WriteLine($"Attempting to authenticate to Maker Portal: {_xrmUri}");

                            for (int retryCount = 0; retryCount < Reference.Login.SignInAttempts; retryCount++)
                            {
                                try
                                {
                                    appBrowser.OnlineLogin.Login(_xrmUri, _username.ToSecureString(), _password.ToSecureString());
                                    break;
                                }
                                catch (Exception exc)
                                {
                                    Console.WriteLine($"Exception on Attempt #{retryCount + 1}: {exc}");

                                    if (retryCount + 1 == Reference.Login.SignInAttempts)
                                    {
                                        // Login exception occurred, take screenshot
                                        _resultsDirectory = TestContext.TestResultsDirectory;
                                        string location = $@"{_resultsDirectory}\RunTestAutomation-LoginErrorAttempt{retryCount + 1}.jpeg";

                                        appBrowser.TakeWindowScreenShot(location, OpenQA.Selenium.ScreenshotImageFormat.Jpeg);
                                        _testContext.AddResultFile(location);

                                        // Max Sign-In Attempts reached
                                        Console.WriteLine($"Login failed after {retryCount + 1} attempts.");
                                        throw new InvalidOperationException($"Login failed after {retryCount + 1} attempts. Exception Details: {exc}");
                                    }
                                    else
                                    {
                                        // Login exception occurred, take screenshot
                                        _resultsDirectory = TestContext.TestResultsDirectory;
                                        string location = $@"{_resultsDirectory}\RunTestAutomation-LoginErrorAttempt{retryCount + 1}.jpeg";

                                        appBrowser.TakeWindowScreenShot(location, OpenQA.Selenium.ScreenshotImageFormat.Jpeg);
                                        _testContext.AddResultFile(location);

                                        //Navigate away and retry
                                        appBrowser.Navigate("about:blank");

                                        Console.WriteLine($"Login failed after attempt #{retryCount + 1}.");
                                        continue;
                                    }
                                }
                            }
                        }

                        Console.WriteLine($"Power Apps  Test Automation Execution Starting Test #{testRunCounter} of {testUrlCount}");

                        // Initialize TestFrameworok results JSON object
                        JObject testAutomationResults = new JObject();

                        // Execute TestAutomation and return JSON result object
                        testAutomationResults = appBrowser.TestAutomation.ExecuteTestAutomation(_testAutomationUri, testRunCounter, _testMaxWaitTimeInSeconds);

                        #if DEBUG    
                        // Only output post execution screenshot in debug mode
                        _resultsDirectory = TestContext.TestResultsDirectory;
                        string location1 = $@"{_resultsDirectory}\TestRun{testRunCounter}-PostExecutionScreenshot.jpeg";
                        appBrowser.TakeWindowScreenShot(location1, OpenQA.Selenium.ScreenshotImageFormat.Jpeg);
                        _testContext.AddResultFile(location1);
                        #endif

                        // Report Results to DevOps Pipeline                    
                        var testResultCount = appBrowser.TestAutomation.ReportResultsToDevOps(testAutomationResults, testRunCounter);

                        _globalPassCount += testResultCount.Item1;
                        _globalFailCount += testResultCount.Item2;
                        _globalTestCount += (testResultCount.Item1 + testResultCount.Item2);

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"An error occurred during Test Run #{testRunCounter} of {testUrlCount}: {e}");

                        _resultsDirectory = TestContext.TestResultsDirectory;
                        Console.WriteLine($"Current results directory location: {_resultsDirectory}");
                        string location = $@"{_resultsDirectory}\TestRun{testRunCounter}-GenericError.jpeg";

                        appBrowser.TakeWindowScreenShot(location, OpenQA.Selenium.ScreenshotImageFormat.Jpeg);
                        _testContext.AddResultFile(location);

                        throw;
                    }

                    Console.WriteLine($"Power Apps  Test Automation Execution Completed for Test Run #{testRunCounter} of {testUrlCount}." + "\n");
                }

                
                if (_globalPassCount > 0 && _globalFailCount > 0)
                {
                    string message = ("\n" 
                        + "Inconclusive Test Automation Result: " + "\n"
                        + $"Total Test Count: {_globalTestCount}" + "\n"
                        + $"Total Pass Count: {_globalPassCount}" + "\n" 
                        + $"Total Fail Count: {_globalFailCount}" + "\n" 
                        + "Please see the console log for more information.");
                    Assert.Fail(message);
                }
                else if (_globalFailCount > 0)
                {
                    string message = ("\n" 
                        + "Test Failed: " + "\n" 
                        + $"Total Fail Count: {_globalFailCount}" + "\n" 
                        + "Please see the console log for more information.");
                    Assert.Fail(message);
                }
                else if (_globalPassCount > 0)
                {
                    var success = true;
                    string message = ("\n" 
                        + "Success: " + "\n" 
                        + $"Total Pass Count: {_globalPassCount}");
                    Assert.IsTrue(success, message);
                }
                
            }
        }        
    }
}
