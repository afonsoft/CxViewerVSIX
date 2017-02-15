﻿using System;
using Common;
using System.IO;
using System.Net;
using System.Text;
using CxViewerAction.Entities;
using CxViewerAction.ValueObjects;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CxViewerAction.Helpers;

namespace CxViewerAction.Services
{
    public class CxRESTApiLogin
    {
        #region Fields

        private LoginData _login;
        private string _restApplicationAPIRelativePath = "/cxrestapi/auth/login";
        private string _restSSOLoginAPIRelativePath = "/cxrestapi/auth/ssologin";
        private const string requestContentType = "application/x-www-form-urlencoded; charset=UTF-8";
        private const string _messageBodyTemplate = "username={0}&password={1}";

        #endregion

        #region Ctor

        public CxRESTApiLogin(LoginData login)
        {
            _login = login;
        }

        #endregion

        #region API

        public CxRESTApiLoginResponse Login()
        {
            CxRESTApiLoginResponse cxRESTApiLoginResponse = new CxRESTApiLoginResponse();

            try
            {
                Uri uri = GetLoginUri();
                string messageBody = GetMesageBody();
                byte[] messageBodyAsByteArray = GetMesageBodyEncoded();
                HttpWebRequest webRequest = CreateWebRequest(uri, messageBody, messageBodyAsByteArray);
                HandleWebResponse(cxRESTApiLoginResponse, webRequest);
            }
            catch (Exception ex)
            {
                Logger.Create().Error("CxRESTApiLogin->Login: " + ex.ToString());
                throw new WebException(ex.ToString());
            }

            return cxRESTApiLoginResponse;
        }

        #endregion

        #region Private methods

        private HttpWebRequest GetWebRequest(Uri uri, string messageBody)
        {
            HttpWebRequest webRequest = new CxRESTApiWebRequestCore().Create(uri, "POST");
            webRequest.ContentType = requestContentType;
            webRequest.ContentLength = messageBody.Length;

            return webRequest;
        }

        private HttpWebRequest CreateWebRequest(Uri uri, string messageBody, byte[] messageBodyAsByteArray)
        {
            HttpWebRequest webRequest = GetWebRequest(uri, messageBody);

            using (Stream requestStream = webRequest.GetRequestStream())
            {
                requestStream.Write(messageBodyAsByteArray, 0, messageBody.Length);
                requestStream.Close();
            }

            return webRequest;
        }

        private void HandleWebResponse(CxRESTApiLoginResponse cxRESTApiLoginResponse, HttpWebRequest webRequest)
        {
            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
            cxRESTApiLoginResponse.ResponseStatusCode = webResponse.StatusCode;

            if (webResponse.StatusCode != HttpStatusCode.OK)
            {
                Logger.Create().Error("CxRESTApiLogin->Login->Rest API, status message: " + webResponse.StatusDescription);
            }
            else
            {
                cxRESTApiLoginResponse.IsSuccessful = true;
            }

            Common.Web.Cookies.FillWebReponseCookies(webRequest, webResponse);
            LoginHelper.RESTApiCookies = webResponse.Cookies;
            new CxRESTApiPortalConfiguration().InitPortalBaseUrl();

            Common.Web.Cookies.SetCookiesInTheInternalBrowser(webResponse, LoginHelper.PortalConfiguration.WebServer);
        }

        private string GetMesageBody()
        {
            return string.Format(_messageBodyTemplate, _login.UserName, _login.Password);
        }

        private byte[] GetMesageBodyEncoded()
        {
            string messageBody = GetMesageBody();
            return Encoding.UTF8.GetBytes(messageBody);
        }

        private Uri GetLoginUri()
        {
            string relativeUrl = string.Empty;

            if (_login.SSO)
            {
                relativeUrl = _restSSOLoginAPIRelativePath;
            }
            else if (!_login.isSaml && !_login.SSO)
            {
                relativeUrl = _restApplicationAPIRelativePath;
            }

            string url = string.Format("{0}{1}", Common.Text.Text.RemoveTrailingSlash(_login.ServerBaseUri), relativeUrl);

            Uri uri = new Uri(url);

            return uri;
        }

        #endregion
    }
}
