// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerApps.TestAutomation.Api
{
    public static class Elements
    {
        public static Dictionary<string, string> Xpath = new Dictionary<string, string>()
        {               
            //Login           
            { "Login_UserId", "//input[@type='email']"},
            { "Login_Password", "//input[@type='password']"},
            { "Login_SignIn", "id(\"cred_sign_in_button\")"},
            { "Login_MainPage", "//div[contains(@class,\"home-page-component\")]"},
            { "Login_StaySignedIn", "id(\"idSIButton9\")"},

            //TestAutomation
            { "TestAutomation_ToastMessage", "//*[@class=\"toast-message\"]" },
            { "TestAutomation_PermissionDialogButtons", "//*[@class=\"button-strip\"]" },

        };

        public static Dictionary<string, string> ElementId = new Dictionary<string, string>()
        {

        };

        public static Dictionary<string, string> CssClass = new Dictionary<string, string>()
        {

        };

        public static Dictionary<string, string> Name = new Dictionary<string, string>()
        {

        };
    }

    public static class Reference
    {
        public static class Login
        {
            public static string UserId = "Login_UserId";
            public static string LoginPassword = "Login_Password";
            public static string SignIn = "Login_SignIn";
            public static string MainPage = "Login_MainPage";
            public static string StaySignedIn = "Login_StaySignedIn";
            public static int SignInAttempts = 3;
        }

        public static class TestAutomation
        {
            public static string ToastMessage = "TestAutomation_ToastMessage";
            public static string PermissionDialogButtons = "TestAutomation_PermissionDialogButtons";
        }
    }
}

