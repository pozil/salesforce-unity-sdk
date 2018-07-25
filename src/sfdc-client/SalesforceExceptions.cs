using System;

namespace SFDC
{
    /**
     * This is a generic exception that can be throw when using SalesforceClient
     */
    public class SalesforceApiException : Exception {
        public SalesforceApiException(string message) : base(message) {
        }

        public SalesforceApiException(string message, Exception inner) : base(message, inner) {
        }
    }

    /**
     * This exception is thrown when the OAuth configuration is invalid
     */
    public class SalesforceConfigurationException : Exception {
        public SalesforceConfigurationException(string message) : base(message) {
        }

        public SalesforceConfigurationException(string message, Exception inner) : base(message, inner) {
        }
    }

    /**
     * This exception is thrown when the user credentials are invalid (username, password, personal security token)
     **/
    public class SalesforceAuthenticationException : Exception {
        public SalesforceAuthenticationException(string message) : base(message) {
        }

        public SalesforceAuthenticationException(string message, Exception inner) : base(message, inner) {
        }
    }
}
