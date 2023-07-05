using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using System;
using System.Net;
using System.IO;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Sockets;

//=====================================================================================================================
//	eConst
//---------------------------------------------------------------------------------------------------------------------
public static class eConst
{
    public const string CLIENT_ID                 = "auth-test";
    public const string CLIENT_SECRET             = "1mFnix5xk53Q620Jvn1XLecMluAwDQ9W";
    public const string CLIENT_SCOPES             = "openid email";

    #if UNITY_STANDALONE_WIN
    public const string BASE_REDIRECT_URI         = "http://localhost:8443/";
    public const string REDIRECT_URI              = "http://localhost:8443/auth/hyper-id/callback/";
    #endif

    #if UNITY_ANDROID
    public const string BASE_REDIRECT_URI         = "com.deeplink.sample://com.deeplink.sample.com/auth/hyper-id/callback/";
    public const string REDIRECT_URI              = "com.deeplink.sample://com.deeplink.sample.com/auth/hyper-id/callback/";
    #endif

    public const string AUTH_URL                  = "https://login-sandbox.hypersecureid.com/auth/realms/HyperID/protocol/openid-connect/auth";
    public const string ACCESS_TOKEN_URL          = "https://login-sandbox.hypersecureid.com/auth/realms/HyperID/protocol/openid-connect/token";
    public const string LOGOUT_URL                = "https://login-sandbox.hypersecureid.com/auth/realms/HyperID/protocol/openid-connect/logout";

    public const string RESPONSE_TYPE_CODE        = "code";
    public const string REDIRECT_TYPE_CODE        = "code";
    public const string GRANT_TYPE_AUTH_CODE      = "authorization_code";
    public const string GRANT_TYPE_REFRESH_TOKEN  = "refresh_token";
}
//=====================================================================================================================
//	eAccessTokenResponse
//---------------------------------------------------------------------------------------------------------------------
[System.Serializable]
public class eAccessTokenResponse
{
    public string access_token;
    public string refresh_token;
}
//=====================================================================================================================
//	eRedirectUriListener
//---------------------------------------------------------------------------------------------------------------------
#if UNITY_STANDALONE_WIN
public class eRedirectUriListener : MonoBehaviour
{
    public string url  { get; set; }
    
    HttpListener httpListener;

    public void StartListening()
    {
        httpListener = new HttpListener();
        httpListener.Prefixes.Add(eConst.BASE_REDIRECT_URI);
        httpListener.Start();
        var context = httpListener.GetContext();
        url = context.Request.RawUrl;
        httpListener.Stop();
    }
}
#endif

//=====================================================================================================================
//	eBrowser
//---------------------------------------------------------------------------------------------------------------------
public class eBrowser : MonoBehaviour
{
    public void Open()
    {
        string url = eConst.AUTH_URL + "?client_id="      + eConst.CLIENT_ID
                                     + "&scope="          + eConst.CLIENT_SCOPES
                                     + "&response_type="  + eConst.RESPONSE_TYPE_CODE
                                     + "&redirect_uri="   + HttpUtility.UrlEncode(eConst.REDIRECT_URI);
        Application.OpenURL(url);
    }
}

//=====================================================================================================================
//	eHyperIDAuthTest
//---------------------------------------------------------------------------------------------------------------------
public class eHyperIdAuthTest : MonoBehaviour
{
    private Button loginButton;
    private Button logoutButton;

    private string accessToken;
    private string refreshToken;

    private bool isLoginStarted = false;

    private void Start()
    {
        loginButton = GameObject.Find("login_button").GetComponent<Button>();
        loginButton.onClick.AddListener(OnLoginButtonClick);

        logoutButton = GameObject.Find("logout_button").GetComponent<Button>();
        logoutButton.onClick.AddListener(OnLogoutButtonClick);

        loginButton.interactable = true;
        logoutButton.interactable = false;
    }

#if UNITY_ANDROID || UNITY_IOS
    private async void OnApplicationFocus(bool focusState)
    {
        if(focusState && isLoginStarted)
        {
            isLoginStarted = false;
            if(!string.IsNullOrEmpty(Application.absoluteURL))
            {
                StartAuthorizationWithUrl(Application.absoluteURL);
            }
        }
    }
#endif

    private async void StartAuthorizationWithUrl(string url)
    {
        string code = url.Split(eConst.REDIRECT_TYPE_CODE)[1].Substring(1);
        await AccessTokenGet(code);
        await AccessTokenRefresh();
        if (accessToken.Length != 0)
        {
            loginButton.interactable = false;
            logoutButton.interactable = true;
        }
    }
    private void Login()
    {
        eBrowser browser = new eBrowser();
        browser.Open();
        isLoginStarted = true;

#if UNITY_STANDALONE_WIN
        eRedirectUriListener uriListener = new eRedirectUriListener();
        uriListener.StartListening();
        StartAuthorizationWithUrl(uriListener.url);
#endif
    }
    private async void OnLoginButtonClick()
    {
        Login();
    }

    private async void OnLogoutButtonClick()
    {
        await Logout();
        loginButton.interactable = true;
        logoutButton.interactable = false;
    }

    public async Task<bool> AccessTokenGet(string _code)
    {
        HttpClient client = new HttpClient();

        Dictionary<string, string> query = new Dictionary<string, string>();
        query["grant_type"]     = eConst.GRANT_TYPE_AUTH_CODE;
        query["client_id"]      = eConst.CLIENT_ID;
        query["code"]           = _code;
        query["redirect_uri"]   = eConst.REDIRECT_URI;
        query["client_secret"]  = eConst.CLIENT_SECRET;

        HttpContent content = new FormUrlEncodedContent(query);

        using HttpResponseMessage response = await client.PostAsync(eConst.ACCESS_TOKEN_URL, content);
        if(response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            eAccessTokenResponse responseJson = JsonUtility.FromJson<eAccessTokenResponse>(responseContent);
            accessToken     = responseJson.access_token;
            refreshToken    = responseJson.refresh_token;
        }
        return response.IsSuccessStatusCode;
    }
    public async Task<bool> AccessTokenRefresh()
    {
        HttpClient client = new HttpClient();

        Dictionary<string, string> query = new Dictionary<string, string>();
        query["grant_type"]     = eConst.GRANT_TYPE_REFRESH_TOKEN;
        query["client_id"]      = eConst.CLIENT_ID;
        query["refresh_token"]  = refreshToken;
        query["redirect_uri"]   = eConst.REDIRECT_URI;
        query["client_secret"]  = eConst.CLIENT_SECRET;

        HttpContent content = new FormUrlEncodedContent(query);

        using HttpResponseMessage response = await client.PostAsync(eConst.ACCESS_TOKEN_URL, content);

        if(response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            eAccessTokenResponse responseJson = JsonUtility.FromJson<eAccessTokenResponse>(responseContent);

            accessToken     = responseJson.access_token;
            refreshToken    = responseJson.refresh_token;
        }
        return response.IsSuccessStatusCode;
    }
    public async Task<bool> Logout()
    {
        HttpClient client = new HttpClient();

        Dictionary<string, string> query = new Dictionary<string, string>();
        query["client_id"]      = eConst.CLIENT_ID;
        query["refresh_token"]  = refreshToken;
        query["client_secret"]  = eConst.CLIENT_SECRET;

        HttpContent content = new FormUrlEncodedContent(query);

        using HttpResponseMessage response = await client.PostAsync(eConst.LOGOUT_URL, content);
        return response.IsSuccessStatusCode;
    }
}