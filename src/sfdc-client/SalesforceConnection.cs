using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Salesforce
{
    public class SalesforceConnection {
        private string token;
        private string instanceUrl;
        private string apiVersion;

        public SalesforceConnection(string token, string instanceUrl, string apiVersion) {
            this.token = token;
            this.instanceUrl = instanceUrl;
            this.apiVersion = apiVersion;
        }

        public string getToken() {
            return token;
        }

        public string getInstanceUrl() {
            return instanceUrl;
        }

        public string getApiVersion() {
            return apiVersion;
        }
    }
}