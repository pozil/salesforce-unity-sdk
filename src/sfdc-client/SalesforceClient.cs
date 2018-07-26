/*
* Salesforce REST API wrapper for Unity 3d.
* v2.0 by Philippe Ozil
* https://github.com/pozil/salesforce-unity-sdk
*/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;
using UnityEngine.Networking;
using System.Reflection;

namespace Salesforce
{
    public class SalesforceClient : MonoBehaviour {

        [Tooltip("If enabled, API responses are traced in console")]
        public Boolean isDebugMode = false;

        [Tooltip("Salesforce REST API version")]
        public string apiVersion = "v43.0";

        [Header("Authentication Settings (keep those secret!)")]
        [Tooltip("Change default value when using a sandbox or restricting authentication domain")]
        public string oAuthEndpoint = "https://login.salesforce.com/services/oauth2/token";
        [Tooltip("Get this value from your Salesforce connected application")]
        public string consumerKey = "";
        [Tooltip("Get this value from your Salesforce connected application")]
        public string consumerSecret = "";

        private SalesforceConnection connection;

        /**
         * Allows to resume an already open connection and save time by not logging in again.
         */
        public void setConnection(SalesforceConnection connection) {
            this.connection = connection;
        }

        public SalesforceConnection getConnection() {
            return connection;
        }

        /*
        * @description Authenticates the user with Salesforce via a connected OAuth application.
        *
        * @param username The user's Salesforce username
        * @param password The user's Salesforce password
        *
        * @throws SalesforceConfigurationException if client is not properly configured
        * @throws SalesforceAuthenticationException if authentication request fails due to invalid credentials
        * @throws SalesforceApiException if authentication request fails (possible reasons: network...)
        */
        public IEnumerator login(string username, string password) {
            // Check configuration
            assertConfigurationIsValid(username, password);

            // Check if Auth Token is already set
            if (isUserLoggedIn()) {
                yield return true;
            }

            // Configure request
            WWWForm form = new WWWForm();
            form.AddField("username", username);
            form.AddField("password", password);
            form.AddField("client_secret", consumerSecret);
            form.AddField("client_id", consumerKey);
            form.AddField("grant_type", "password");

            // Send request & parse response
            using (UnityWebRequest request = UnityWebRequest.Post(oAuthEndpoint, form)) {
                yield return request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError) {
                    logResponseError(request);
                    JSONObject errorDetails = JSONObject.Parse(request.downloadHandler.text);
                    if (errorDetails != null) {
                        string error = errorDetails.GetString("error");
                        string errorDescription = errorDetails.GetString("error_description");
                        if (error == "invalid_client_id" || error == "invalid_client")
                            throw new SalesforceConfigurationException("Salesforce authentication error due to invalid OAuth configuration: " + errorDescription);
                        else if (error == "invalid_grant")
                            throw new SalesforceAuthenticationException("Salesforce authentication error due to invalid user credentials: " + errorDescription);
                        else
                            throw new SalesforceApiException("Salesforce authentication error: " + errorDescription);
                    }
                    throw new SalesforceApiException("Salesforce authentication error: " + request.error);
                }
                else {
                    logResponseSuccess(request);
                    JSONObject obj = JSONObject.Parse(request.downloadHandler.text);
                    string token = obj.GetString("access_token");
                    string instanceUrl = obj.GetString("instance_url");
                    connection = new SalesforceConnection(token, instanceUrl, apiVersion);
                    yield return true;
                }
            }
        }

        /**
        * @description Checks if user is logged in
        */
        public Boolean isUserLoggedIn() {
            return connection != null;
        }

        /*
        * @description Executes a SOQL query against Salesforce
        *
        * @param query The SOQL query to be executed
        * @return JSON string with query results
        * @throws SalesforceApiException if query fails
        */
        public IEnumerator query(string query) {
            assertUserIsLoggedIn();

            string url = getDataServiceUrl() +"query?q="+ UnityWebRequest.EscapeURL(query);
            using (UnityWebRequest request = getBaseRequest(url, "GET")) {
                yield return request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError) {
                    logResponseError(request);
                    throw new SalesforceApiException("Salesforce query error: " + request.error);
                }
                else {
                    logResponseSuccess(request);
                    yield return request.downloadHandler.text;
                }
            }
        }

        /*
        * @description Executes a SOQL query against Salesforce and returns records of a given type
        *
        * @param query The SOQL query to be executed
        * @return a list of records of the given type
        * @throws SalesforceApiException if query fails
        */
        public IEnumerator query<T>(string query) where T : SalesforceRecord, new() {
            assertUserIsLoggedIn();

            string url = getDataServiceUrl() + "query?q=" + UnityWebRequest.EscapeURL(query);
            using (UnityWebRequest request = getBaseRequest(url, "GET")) {
                yield return request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError) {
                    logResponseError(request);
                    throw new SalesforceApiException("Salesforce query error: " + request.error);
                }
                else {
                    logResponseSuccess(request);
                    JSONObject json = JSONObject.Parse(request.downloadHandler.text);
                    JSONArray recordsJson = json.GetArray("records");
                    yield return SalesforceRecord.parseFromJsonArray<T>(recordsJson);
                }
            }
        }


        /*
        * @description Inserts a Salesforce record.
        *
        * @param record The record
        * @return the record with its new id
        * @throws SalesforceApiException if request fails
        */
        public IEnumerator insert(SalesforceRecord record) {
            assertUserIsLoggedIn();

            JSONObject recordJson = record.toJson();
            string url = getDataServiceUrl() + "sobjects/" + record.getSObjectName();

            using (UnityWebRequest request = getBaseRequest(url, "POST")) {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(recordJson.ToString()));
                yield return request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError) {
                    logResponseError(request);
                    throw new SalesforceApiException("Salesforce insert error: " + request.error);
                }
                else {
                    logResponseSuccess(request);
                    JSONObject jsonResponse = JSONObject.Parse(request.downloadHandler.text);
                    record.id = jsonResponse.GetString("id");
                    yield return record;
                }
            }
        }

        /*
        * @description Updates a Salesforce record.
        *
        * @param record The record
        * @return an empty string
        * @throws SalesforceApiException if request fails
        */
        public IEnumerator update(SalesforceRecord record) {
            assertUserIsLoggedIn();

            JSONObject recordJson = record.toJson();
            recordJson.Remove("Id");

            string url = getDataServiceUrl() + "sobjects/" + record.getSObjectName() + "/" + record.id;
            using (UnityWebRequest request = getBaseRequest(url, "PATCH")) {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(recordJson.ToString()));
                yield return request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError) {
                    logResponseError(request);
                    throw new SalesforceApiException("Salesforce update error: " + request.error);
                }
                else {
                    logResponseSuccess(request);
                    yield return request.downloadHandler.text;
                }
            }
        }

        /*
        * @description Deletes a Salesforce record.
        *
        * @param record The record
        * @return an empty string
        * @throws SalesforceApiException if request fails
        */
        public IEnumerator delete(SalesforceRecord record) {
            assertUserIsLoggedIn();

            string url = getDataServiceUrl() + "sobjects/" + record.getSObjectName() + "/" + record.id;
            using (UnityWebRequest request = getBaseRequest(url, "DELETE")) {
                yield return request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError) {
                    logResponseError(request);
                    throw new SalesforceApiException("Salesforce delete error: " + request.error);
                }
                else {
                    logResponseSuccess(request);
                    yield return request.downloadHandler.text;
                }
            }
        }

        /*
        * @description Executes a custom Apex Method via a custom REST endpoint
        * See docs: https://developer.salesforce.com/docs/atlas.en-us.apexcode.meta/apexcode/apex_rest_code_sample_basic.htm
        *
        * @param httpMethod HTTP method: GET, POST, PATCH, PUT or DELETE
        * @param restResource mapping for this REST Resource (see @RestResource class annotation in Apex code)
        * @param body Optional request body
        * @param queryString Optional query string
        * @throws SalesforceApiException if request fails
        */
        public IEnumerator runApex(string httpMethod, string restResource, string body, string queryString) {
            assertUserIsLoggedIn();

            string url = connection.getInstanceUrl() +"/services/apexrest/"+ restResource;
            if (queryString != null && !"".Equals(queryString))
                url += "?" + queryString;
            
            using (UnityWebRequest request = getBaseRequest(url, httpMethod)) {
                if (body != null && !"".Equals(body)) {
                    request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
                }
                yield return request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError) {
                    logResponseError(request);
                    throw new SalesforceApiException("Salesforce runApex error: " + request.error);
                }
                else {
                    logResponseSuccess(request);
                    yield return request.downloadHandler.text;
                }
            }
        }

        /*
        * @description Gets the Chatter Feed for the supplied record
        *
        * @param id The Id of the object to get the Chatter Feed from
        * @throws SalesforceApiException if request fails
        */
        public IEnumerator getRecordChatterFeed(string id) {
            assertUserIsLoggedIn();

            string url = getDataServiceUrl() +"chatter/feeds/record/" + id + "/feed-elements";
            using (UnityWebRequest request = getBaseRequest(url, "GET")) {
                yield return request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError) {
                    logResponseError(request);
                    throw new SalesforceApiException("Salesforce getChatterFeed error: " + request.error);
                }
                else {
                    logResponseSuccess(request);
                    yield return request.downloadHandler.text;
                }
            }
        }

        /*
        * @description Posts to a Chatter Feed
        * See docs: https://developer.salesforce.com/docs/atlas.en-us.chatterapi.meta/chatterapi/quickreference_post_feed_item.htm
        *
        * @param body JSON denoting what and where to post to Chatter
        * @throws SalesforceApiException if request fails
        */
        public IEnumerator postToChatter(string body) {
            assertUserIsLoggedIn();

            string url = getDataServiceUrl() +"chatter/feed-elements";
            using (UnityWebRequest request = getBaseRequest(url, "POST")) {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
                yield return request.SendWebRequest();
                if (request.isNetworkError || request.isHttpError) {
                    logResponseError(request);
                    throw new SalesforceApiException("Salesforce postToChatter error: " + request.error);
                }
                else {
                    logResponseSuccess(request);
                    yield return request.downloadHandler.text;
                }
            }
        }

        private void assertConfigurationIsValid(string username, string password) {
            try {
                // Check connected app settings
                assertIsNotNull(oAuthEndpoint, "oAuthEndpoint");
                assertIsNotNull(consumerSecret, "clientSecret");
                assertIsNotNull(consumerKey, "clientId");
                // Check user settings
                assertIsNotNull(username, "username");
                assertIsNotNull(password, "password");
            }
            catch (SalesforceConfigurationException e) {
                throw new SalesforceConfigurationException("Salesforce client is not properly configured: "+ e.Message, e);
            }
        }

        private void assertIsNotNull(string value, string label) {
            if (value == null || "".Equals(value)) {
                throw new SalesforceConfigurationException("Missing value for "+ label);
            }
        }

        private void assertUserIsLoggedIn() {
            if (!isUserLoggedIn()) {
                throw new SalesforceApiException("Cannot perform Salesforce request: not logged in");
            }
        }

        private string getDataServiceUrl() {
            return connection.getInstanceUrl() + "/services/data/" + connection.getApiVersion() + "/";
        }

        private UnityWebRequest getBaseRequest(String url, String httpMethod) {
            UnityWebRequest request = new UnityWebRequest(url);
            request.method = httpMethod;
            request.SetRequestHeader("Authorization", "Bearer " + connection.getToken());
            request.SetRequestHeader("Content-Type", "application/json");
            if (isDebugMode) {
                request.SetRequestHeader("X-PrettyPrint", "1");
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        private void logResponseSuccess(UnityWebRequest request) {
            if (isDebugMode) {
                Debug.Log("Salesforce HTTP request: "+ request.method + " " + request.responseCode + " " + request.url);
                Debug.Log(request.downloadHandler.text);
            }
        }

        private void logResponseError(UnityWebRequest request) {
            Debug.LogError("Salesforce HTTP request: "+ request.method +" "+ request.responseCode +" "+ request.url);
            Debug.LogError(request.error);
            Debug.LogError(request.downloadHandler.text);
        }
    }
}