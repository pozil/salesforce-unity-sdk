/*
* Salesforce REST API wrapper for Unity 3d.
*
* Original code written by John Casimiro on 2014-01-30.
*
* Modified by Ammar Alammar.
*
* Modified by Bobby Tamburrino:
* - removed PlayMaker dependencies
* - added Chatter support
* - added approval process handling
*
* Modified by Philippe Ozil:
* - refactored configuration to separate app/user authentication config
* - switched all request to IEnumerator for more async control
* - added exceptions to handle errors
* - removed response from client class to allow parallel processing
* - added debug mode for improved tracing
*/
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Boomlagoon.JSON;

namespace SFDC
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
        * @throws SalesforceAuthenticationException if authentication request fails due to invalid credetials
        * @throws SalesforceApiException if authentication request fails (possible reasons: network...)
        */
        public IEnumerator login(string username, string password) {
            // Check configuration
            assertConfigurationIsValid(username, password);

            // Check if Auth Token is already set
            if (isUserLoggedIn()) {
                yield return true;
            }

            // Configure query
            WWWForm form = new WWWForm();
            form.AddField("username", username);
            form.AddField("password", password);
            form.AddField("client_secret", consumerSecret);
            form.AddField("client_id", consumerKey);
            form.AddField("grant_type", "password");
            WWW www = new WWW(oAuthEndpoint, form);

            // Execute query & wait for result
            yield return www;

            // Check query result for errors
            if (www.error == null) {
                logHttpResponseSuccess(www, "GET");
                JSONObject obj = JSONObject.Parse(www.text);
                string token = obj.GetString("access_token");
                string instanceUrl = obj.GetString("instance_url");
                connection = new SalesforceConnection(token, instanceUrl, apiVersion);
                yield return true;
            } else {
                logHttpResponseError(www, "GET");
                JSONObject errorDetails = JSONObject.Parse(www.text);
                if (errorDetails != null) {
                    string error = errorDetails.GetString("error");
                    string errorDescription = errorDetails.GetString("error_description");
                    if (error == "invalid_client_id" || error == "invalid_client")
                        throw new SalesforceConfigurationException("Salesforce authentication error due to invalid configuration: " + errorDescription);
                    else if (error == "invalid_grant")
                        throw new SalesforceAuthenticationException("Salesforce authentication error due to invalid user credentials: " + errorDescription);
                    else
                        throw new SalesforceApiException("Salesforce authentication error: " + errorDescription);
                }
                throw new SalesforceApiException("Salesforce authentication error: " + www.error.ToString());
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
        * @param q The SOQL query to be executed
        * @throws SalesforceApiException if query fails
        */
        public IEnumerator query(string q) {
            assertUserIsLoggedIn();

            // Configure query
            string url = connection.getInstanceUrl() + "/services/data/" + connection.getApiVersion() + "/query?q=" + WWW.EscapeURL(q);
            Dictionary<string, string> headers = initRequestHeaders("GET");
            WWW www = new WWW(url, null, headers);

            // Execute query & wait for result
            yield return www;

            // Check query result for errors
            if (www.error == null) {
                logHttpResponseSuccess(www, "GET");
                yield return www.text;
            } else {
                logHttpResponseError(www, "GET");
                throw new SalesforceApiException("Salesforce query error: "+ www.error.ToString());
            }
        }

        /*
        * @description Inserts a record into Salesforce.
        *
        * @param sObjectName The object in salesforce(custom or standard) that you are
        * trying to insert a record to.
        * @param body The JSON for the data(fields and values) that will be inserted.
        * @throws SalesforceApiException if query fails
        */
        public IEnumerator insert(string sObjectName, string body) {
            assertUserIsLoggedIn();

            // Configure query
            string url = connection.getInstanceUrl() + "/services/data/" + connection.getApiVersion() + "/sobjects/" + sObjectName;
            Dictionary<string, string> headers = initRequestHeaders("POST");
            WWW www = new WWW(url, System.Text.Encoding.UTF8.GetBytes(body), headers);

            // Execute query & wait for result
            yield return www;

            // Check query result for errors
            if (www.error == null) {
                logHttpResponseSuccess(www, "POST");
                yield return www.text;
            } else {
                logHttpResponseError(www, "POST");
                throw new SalesforceApiException("Salesforce insert error: "+ www.error.ToString());
            }
        }

        /*
        * @description Updates a record in salesforce.
        *
        * @param id The salesforce id of the record you are trying to update.
        * @param sObjectName The sobject of the record you are trying to update.
        * @param body The JSON for the data(fields and values) that will be updated.
        * @throws SalesforceApiException if query fails
        */
        public IEnumerator update(string id, string sObjectName, string body) {
            assertUserIsLoggedIn();

            // Configure query
            string url = connection.getInstanceUrl() + "/services/data/" + connection.getApiVersion() + "/sobjects/" + sObjectName + "/" + id + "?_HttpMethod=PATCH";
            Dictionary<string, string> headers = initRequestHeaders("POST");
            WWW www = new WWW(url, System.Text.Encoding.UTF8.GetBytes(body), headers);

            // Execute query & wait for result
            yield return www;

            // Check query result for errors
            if (www.error == null) {
                logHttpResponseSuccess(www, "POST");
                yield return www.text;
            } else {
                logHttpResponseError(www, "POST");
                throw new SalesforceApiException("Salesforce update error: "+ www.error.ToString());
            }
        }

        /*
        * @description Deletes a record in salesforce.
        *
        * @param id The salesforce id of the record you are trying to delete.
        * @param sObjectName The sobject of the record you are trying to delete.
        * @throws SalesforceApiException if query fails
        */
        public IEnumerator delete(string id, string sObjectName) {
            assertUserIsLoggedIn();

            // Configure query
            string url = connection.getInstanceUrl() + "/services/data/" + connection.getApiVersion() + "/sobjects/" + sObjectName + "/" + id + "?_HttpMethod=DELETE";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers["Authorization"] = "Bearer " + connection.getToken();
            headers["Method"] = "POST";
            headers["X-PrettyPrint"] = "1";
            // need something in the body for DELETE to work for some reason
            String body = "DELETE";
            WWW www = new WWW(url, System.Text.Encoding.UTF8.GetBytes(body), headers);

            // Execute query & wait for result
            yield return www;

            // Check query result for errors
            if (www.error == null) {
                logHttpResponseSuccess(www, "POST");
                yield return www.text;
            } else {
                logHttpResponseError(www, "POST");
                throw new SalesforceApiException("Salesforce delete error: "+ www.error.ToString());
            }
        }

        /*
        * @description Runs an Apex Remote Method
        *
        * @param method GET or POST
        * @param apexClass RestResource URL Mapping
        * @param body Optional request body
        * @param queryString Optional query string
        * @throws SalesforceApiException if query fails
        */
        public IEnumerator runApex(string httpMethod, string apexClass, string body, string queryString) {
            assertUserIsLoggedIn();

            // Configure query
            string url = connection.getInstanceUrl() + "/services/apexrest/" + apexClass;
            if (queryString != null && !"".Equals(queryString))
                url += "?" + queryString;
            byte[] bodyAsBytes = null;
            if (body != null && !"".Equals(body))
                bodyAsBytes = System.Text.Encoding.UTF8.GetBytes(body);
            Dictionary<string, string> headers = initRequestHeaders(httpMethod);
            WWW www = new WWW(url, bodyAsBytes, headers);

            // Execute query & wait for result
            yield return www;

            // Check query result for errors
            if (www.error == null) {
                logHttpResponseSuccess(www, httpMethod);
                yield return www.text;
            } else {
                logHttpResponseError(www, httpMethod);
                throw new SalesforceApiException("Salesforce runApex error: " + www.error.ToString());
            }
        }

        /*
        * @description Gets the Chatter Feed for the supplied object
        *
        * @param id The Id of the object to get the Chatter Feed from
        * @throws SalesforceApiException if query fails
        */
        public IEnumerator getChatterFeed(string id) {
            assertUserIsLoggedIn();

            // Configure query
            string url = connection.getInstanceUrl() + "/services/data/" + connection.getApiVersion() + "/chatter/feeds/record/" + id + "/feed-elements";
            Dictionary<string, string> headers = initRequestHeaders("POST");
            WWW www = new WWW(url, null, headers);

            // Execute query & wait for result
            yield return www;

            // Check query result for errors
            if (www.error == null) {
                logHttpResponseSuccess(www, "POST");
                yield return www.text;
            } else {
                logHttpResponseError(www, "POST");
                throw new SalesforceApiException("Salesforce getChatterFeed error: " + www.error.ToString());
            }
        }

        /*
        * @description Posts to Chatter
        *
        * @param body JSON denoting what and where to post to Chatter
        * @throws SalesforceApiException if query fails
        */
        public IEnumerator postToChatter(string body) {
            assertUserIsLoggedIn();

            // Configure query
            string url = connection.getInstanceUrl() + "/services/data/" + connection.getApiVersion() + "/chatter/feed-elements";
            Dictionary<string, string> headers = initRequestHeaders("POST");
            WWW www = new WWW(url, System.Text.Encoding.UTF8.GetBytes(body), headers);

            // Execute query & wait for result
            yield return www;

            // Check query result for errors
            if (www.error == null) {
                logHttpResponseSuccess(www, "POST");
                yield return www.text;
            } else {
                logHttpResponseError(www, "POST");
                throw new SalesforceApiException("Salesforce postToChatter error: " + www.error.ToString());
            }
        }

        /*
        * @description Either approves or rejects an Approval Process
        *
        * @param body JSON denoting what Approval Process and what action was taken against it
        * @throws SalesforceApiException if query fails
        */
        public IEnumerator handleApprovalProcess(string body) {
            assertUserIsLoggedIn();

            // Configure query
            string url = connection.getInstanceUrl() + "/services/data/" + connection.getApiVersion() + "/process/approvals";
            Dictionary<string, string> headers = initRequestHeaders("POST");
            WWW www = new WWW(url, System.Text.Encoding.UTF8.GetBytes(body), headers);

            // Execute query & wait for result
            yield return www;

            // Check query result for errors
            if (www.error == null) {
                logHttpResponseSuccess(www, "POST");
                yield return www.text;
            } else {
                logHttpResponseError(www, "POST");
                throw new SalesforceApiException("Salesforce handleApprovalProcess error: " + www.error.ToString());
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
                throw new SalesforceApiException("Cannot perform SFDC query: not logged in");
            }
        }

        private Dictionary<string, string> initRequestHeaders(String httpMethod) {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers["Authorization"] = "Bearer " + connection.getToken();
            headers["Content-Type"] = "application/json";
            headers["Method"] = httpMethod;
            headers["X-PrettyPrint"] = "1";
            return headers;
        }

        private void logHttpResponseSuccess(WWW www, string httpMethod) {
            if (isDebugMode) {
                Debug.Log("Salesforce HTTP request: " + httpMethod + " " + www.url);
                Debug.Log("Response: " + www.text);
            }
        }

        private void logHttpResponseError(WWW www, string httpMethod) {
            Debug.LogError("Salesforce HTTP request: "+ httpMethod +" "+ www.url);
            Debug.LogError(www.error);
            Debug.LogError(www.text);
        }
    }
}