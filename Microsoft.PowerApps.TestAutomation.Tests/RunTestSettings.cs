// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestAutomation.Browser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Configuration;
using System.IO;

namespace Microsoft.PowerApps.TestAutomation.Tests
{
    public class RunTestSettings
    {
        public TestContext TestContext { get; set; }
        private static TestContext _testContext;
        private static BrowserType Type;
        private static string DriversPath;
        private static bool? UsePrivateMode;

       [ClassInitialize]
        public static void Initialize(TestContext TestContext)
        {
            _testContext = TestContext;
            Type = (BrowserType)Enum.Parse(typeof(BrowserType), _testContext.Properties["BrowserType"].ToString());
            DriversPath = _testContext.Properties["DriversPath"].ToString();
            UsePrivateMode = Convert.ToBoolean(_testContext.Properties["UsePrivateMode"].ToString());
        }

        public static BrowserOptions Options = new BrowserOptions
        {
            BrowserType = Type,
            PrivateMode = UsePrivateMode ?? true,
            FireEvents = false,
            Headless = false,
            UserAgent = false,
            //DriversPath = Path.IsPathRooted(DriversPath) ? DriversPath : Path.Combine(Directory.GetCurrentDirectory(), DriversPath)

        };
    }
}