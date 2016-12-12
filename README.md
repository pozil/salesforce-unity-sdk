<img align="right" src="/media/salesforce-unity-sdk.png?raw=true" width="25%" alt="Salesforce Unity SDK"/>

# Salesforce Unity SDK

## About
This project is a [Salesforce](https://www.salesforce.com) SDK for the popular game engine [Unity 3d](https://unity3d.com/).<br/>
The SDK is written in C# and interact with the Force.com REST APIs.

It provides the following services:
- OAuth authentication
- Salesforce records query, insert, update, delete operations
- Apex remote methods calls
- Chatter feed interactions such as retrieving a feed and posting to it
- Approval process interactions

This SDK is provided “as is“ without any warranty or support. Salesforce does not officially endorse it.

## Dependencies

**JSONObject**

This project relies on JSONObject 1.4, a simple C# JSON parser.<br/>
Copyright (C) 2012 Boomlagoon Ltd.

**Coroutine extensions**

As a convenience, this project includes **optional** utility classes that provide Coroutines with supports for return values and exceptions.
```C#
Coroutine<bool> routine = this.StartCoroutine<bool>(sfClient.login(sfdcUsername, sfdcPassword, sfdcPersonalSecurityToken));
yield return routine.coroutine;
try {
  isUserLogged = routine.getValue();
}
catch (SalesforceApiException e) {
  Debug.Log("Salesforce login failed: "+ e.Message, e);
}
```

Credit for this Coroutine utility goes to @horsman from Twisted Oak Studios.

## Credits
Original code written by John Casimiro on 2014-01-30

Modified by Ammar Alammar

Modified by Bobby Tamburrino:
- removed PlayMaker dependencies
- added Chatter support
- added approval process handling

Modified by Philippe Ozil:
- refactored configuration to separate app/user authentication config
- switched all request to IEnumerator for more async control
- added exceptions to handle errors
- removed response from client class to allow parallel processing
- added debug mode for improved tracing
