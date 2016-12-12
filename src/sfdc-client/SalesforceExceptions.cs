using System;

namespace SFDC
{
	public class SalesforceApiException : Exception
	{
		public SalesforceApiException(string message) : base(message)
        {
		}

		public SalesforceApiException(string message, Exception inner) : base(message, inner)
		{
		}
	}

    public class SalesforceConfigurationException : Exception
    {
        public SalesforceConfigurationException(string message) : base(message)
        {
        }

        public SalesforceConfigurationException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}

