// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestAutomation.Browser;
using OpenQA.Selenium;
using System;

namespace Microsoft.PowerApps.TestAutomation.Api
{
    /// <summary>
    /// Provides API methods to simulate user interaction with the Dynamics 365 application. 
    /// </summary>
    /// <seealso cref="Microsoft.PowerApps.TestAutomation.Browser.InteractiveBrowser" />
    public class PowerAppBrowser : InteractiveBrowser
    {
        #region Constructor(s)

        internal PowerAppBrowser(IWebDriver driver) : base(driver)
        {
        }

        public PowerAppBrowser(BrowserType type) : base(type)
        {
        }

        public PowerAppBrowser(BrowserOptions options) : base(options)
        {

        }

        #endregion Constructor(s)

        #region Login

        public OnlineLogin OnlineLogin => this.GetPage<OnlineLogin>();

        public void GoToXrmUri(Uri xrmUri)
        {
            this.Driver.Navigate().GoToUrl(xrmUri);
            this.Driver.WaitForPageToLoad();
        }

        #endregion Login

        #region TestAutomation
        public TestAutomation TestAutomation => this.GetPage<TestAutomation>();
        #endregion

    }
}
