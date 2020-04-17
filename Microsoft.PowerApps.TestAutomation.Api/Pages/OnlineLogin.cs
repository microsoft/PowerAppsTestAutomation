// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.PowerApps.TestAutomation.Browser;
using OpenQA.Selenium;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Threading;

namespace Microsoft.PowerApps.TestAutomation.Api
{
    public enum LoginResult
    {
        Success,
        Failure,
        Redirect
    }

    /// <summary>
    /// Login Page
    /// </summary>
    public class OnlineLogin
        : AppPage
    {
        public string[] OnlineDomains { get; set; }

        public OnlineLogin(InteractiveBrowser browser)
            : base(browser)
        {
            this.OnlineDomains = Constants.Xrm.XrmDomains;
        }

        public OnlineLogin(InteractiveBrowser browser, params string[] onlineDomains)
            : this(browser)
        {
            this.OnlineDomains = onlineDomains;
        }

        public BrowserCommandResult<LoginResult> Login()
        {
            return this.Login(new Uri(Constants.DefaultLoginUri));
        }

        public BrowserCommandResult<LoginResult> Login(SecureString username, SecureString password)
        {
            return this.Execute(GetOptions("Login"), this.Login, new Uri(Constants.DefaultLoginUri), username, password, default(Action<LoginRedirectEventArgs>));
        }

        public BrowserCommandResult<LoginResult> Login(SecureString username, SecureString password, Action<LoginRedirectEventArgs> redirectAction)
        {
            return this.Execute(GetOptions("Login"), this.Login, new Uri(Constants.DefaultLoginUri), username, password, redirectAction);
        }

        public BrowserCommandResult<LoginResult> Login(Uri uri)
        {
            if (this.Browser.Options.Credentials.Username == null)
                return PassThroughLogin(uri);
            else // Local Testing Scenario
                return this.Execute(GetOptions("Login"), this.Login, uri, this.Browser.Options.Credentials.Username, this.Browser.Options.Credentials.Password, default(Action<LoginRedirectEventArgs>));
        }

        /// <summary>
        /// Login Page
        /// </summary>
        /// <param name="uri">The Uri</param>
        /// <param name="username">The Username to login to CRM application</param>
        /// <param name="password">The Password to login to CRM application</param>
        /// <example>xrmBrowser.OnlineLogin.Login(_xrmUri, _username, _password);</example>
        public BrowserCommandResult<LoginResult> Login(Uri uri, SecureString username, SecureString password)
        {
            return this.Execute(GetOptions("Login"), this.Login, uri, username, password, default(Action<LoginRedirectEventArgs>));
        }

        /// <summary>
        /// Login Page
        /// </summary>
        /// <param name="uri">The Uri</param>
        /// <param name="username">The Username to login to CRM application</param>
        /// <param name="password">The Password to login to CRM application</param>
        /// <param name="redirectAction">The RedirectAction</param>
        /// <example>xrmBrowser.OnlineLogin.Login(_xrmUri, _username, _password, ADFSLogin);</example>
        public BrowserCommandResult<LoginResult> Login(Uri uri, SecureString username, SecureString password, Action<LoginRedirectEventArgs> redirectAction)
        {
            return this.Execute(GetOptions("Login"), this.Login, uri, username, password, redirectAction);
        }

        private LoginResult Login(IWebDriver driver, Uri uri, SecureString username, SecureString password, Action<LoginRedirectEventArgs> redirectAction)
        {
            var redirect = false;
            // bool online = !(this.OnlineDomains != null && !this.OnlineDomains.Any(d => uri.Host.EndsWith(d)));
            driver.Navigate().GoToUrl(uri);

            if (driver.IsVisible(By.Id("use_another_account_link")))
                driver.ClickWhenAvailable(By.Id("use_another_account_link"));

            // Attempt to locate the UserId field
            driver.WaitUntilAvailable(By.XPath(Elements.Xpath[Reference.Login.UserId]));
            driver.WaitUntilVisible(By.XPath(Elements.Xpath[Reference.Login.UserId]));

            var userIdFieldVisible = driver.IsVisible(By.XPath(Elements.Xpath[Reference.Login.UserId]));
            Debug.WriteLine($"Value of userIdFieldVisible: {userIdFieldVisible}");

            if (userIdFieldVisible)
            {
                Debug.WriteLine("UserID field is visible. Proceeding with login.");
                driver.FindElement(By.XPath(Elements.Xpath[Reference.Login.UserId])).SendKeys(username.ToUnsecureString());
                driver.FindElement(By.XPath(Elements.Xpath[Reference.Login.UserId])).SendKeys(Keys.Tab);
                driver.FindElement(By.XPath(Elements.Xpath[Reference.Login.UserId])).SendKeys(Keys.Enter);

                Thread.Sleep(2000);

                //If expecting redirect then wait for redirect to trigger
                if (redirectAction != null)
                {
                    //Wait for redirect to occur.
                    Thread.Sleep(3000);

                    redirectAction?.Invoke(new LoginRedirectEventArgs(username, password, driver));

                    redirect = true;
                }
                else
                {
                    Thread.Sleep(1000);

                    driver.FindElement(By.XPath(Elements.Xpath[Reference.Login.LoginPassword])).SendKeys(password.ToUnsecureString());
                    driver.FindElement(By.XPath(Elements.Xpath[Reference.Login.LoginPassword])).SendKeys(Keys.Tab);
                    driver.FindElement(By.XPath(Elements.Xpath[Reference.Login.LoginPassword])).Submit();

                    Thread.Sleep(1000);

                    var staySignedInVisible = driver.WaitUntilVisible(By.XPath(Elements.Xpath[Reference.Login.StaySignedIn]), new TimeSpan(0, 0, 5));

                    if (staySignedInVisible)
                    {
                        driver.ClickWhenAvailable(By.XPath(Elements.Xpath[Reference.Login.StaySignedIn]));
                    }

                    driver.WaitUntilVisible(By.XPath(Elements.Xpath[Reference.Login.MainPage])
                        , new TimeSpan(0, 2, 0),
                        e =>
                        {
                            try
                            {
                                e.WaitUntilVisible(By.ClassName("apps-list"), new TimeSpan(0, 0, 30));
                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine("The Maker Portal Apps List did not return visible.");
                                throw new InvalidOperationException($"The Maker Portal Apps List did not return visible.: {exc}");
                            }

                            e.WaitForPageToLoad();
                        },
                        f =>
                        {
                            Console.WriteLine("Login.MainPage failed to load in 2 minutes using Cloud Identity Login.");
                            throw new Exception("Login page failed using Cloud Identity Login.");
                        });
                }
            }
            else
            {
                Console.WriteLine("UserID field is not visible. This should indicate a previous main page load failure.");

                // This scenario should only be hit in the event of a login.microsoftonline.com failure, or a login retry authentication where an authentication token was already retrieved
                driver.WaitUntilVisible(By.XPath(Elements.Xpath[Reference.Login.MainPage])
                    , new TimeSpan(0, 2, 0),
                    e =>
                    {
                        try
                        {
                            e.WaitUntilVisible(By.ClassName("apps-list"), new TimeSpan(0, 0, 30));
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("The Maker Portal Apps List did not return visible.");
                            throw new InvalidOperationException($"The Maker Portal Apps List did not return visible.: {exc}");
                        }

                        e.WaitForPageToLoad();
                    },
                    f =>
                    {
                        Console.WriteLine("Login.MainPage failed to load in 2 minutes on login retry.");
                        throw new Exception("Login page failed on login retry.");
                    });
            }

            return redirect ? LoginResult.Redirect : LoginResult.Success;
        }

        internal BrowserCommandResult<LoginResult> PassThroughLogin(Uri uri)
        {
            return this.Execute(GetOptions("Pass Through Login"), driver =>
            {
                driver.Navigate().GoToUrl(uri);

                driver.WaitUntilVisible(By.XPath(Elements.Xpath[Reference.Login.MainPage])
                                    , new TimeSpan(0, 3, 0),
                                    e =>
                                    {
                                        try
                                        {
                                            e.WaitUntilVisible(By.ClassName("apps-list"), new TimeSpan(0, 0, 30));
                                        }
                                        catch (Exception exc)
                                        {
                                            Console.WriteLine("The Maker Portal Apps List did not return visible.");
                                            throw new InvalidOperationException($"The Maker Portal Apps List did not return visible.: {exc}");
                                        }

                                        e.WaitForPageToLoad();
                                    },
                                    f =>
                                    {
                                        Console.WriteLine("Login.MainPage failed to load in 2 minutes using PassThrough login.");
                                        throw new Exception("Login page failed using PassThrough login.");
                                    });

                return LoginResult.Success;
            });
        }
    }
}