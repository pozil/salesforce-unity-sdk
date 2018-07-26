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

This SDK is provided “as is“ without any warranty or support. Salesforce does not officially endorse it.

## Installation

### Sign up for a Salesforce Org (optional)
If you need a Salesforce Org, you can sign up for a free [Salesforce Developer Edition](https://developer.salesforce.com/signup) (DE) organization.

#### Create a Connected App in Salesforce
1. Log in to your Salesforce org.
2. At the top right of the page, select the gear icon and then click **Setup**.
3. From Setup, enter `App Manager` in the Quick Find and select **App Manager**.
4. Click **New Connected App**.
5. Enter `Unity 3d API` as the **Connected App Name**
6. Enter your **Contact Email**.
7. Under **API (Enable OAuth Settings)**, check the **Enable OAuth Settings** checkbox.
8. Enter `https://localhost/` as the **Callback URL**.
9. Under **Selected OAuth Scope**, move **Access and manage your data (API)** to the Selected OAuth Scopes list.
10. Click **Save**.
11. From this screen, copy the connected app’s **Consumer Key** and **Consumer Secret** some place temporarily.

### Install the Salesforce Client in Unity
1. Copy the content of the `src` directory into your Unity project.
2. Add the `Salesforce Client` component to your scene.
3. Configure the client with the **Consumer Key** and **Consumer Secret** you obtained earlier.

If you want to test the client, follow these extra instructions:
1. Add the `Test Salesforce Client` component on the same object that contains the `Salesforce Client` component
2. Configure the test component with your Salesforce credentials
3. Start the game and watch the Unity console

If you do not want to use the sample code, you can safely remove the `sample` directory.

## Documentation

The `TestSalesforceClient` class highlights the following:
- how to log in to Salesforce and how to diagnose client configuration errors
- how to perform basic CRUD operations on Salesforce records (Case object in this sample).

Note that all Salesforce Client operations require that you log in to Salesforce beforehand.

### Support for standard or custom Salesforce objects
In order to support any standard or custom Salesforce objects, you must create a class per object.<br/>
This class must inherit from `SalesforceRecord`. See the `Case` class for example.

### Calling a custom Apex REST method
Assuming that you have deployed the following Apex class in Salesforce:
```apex
@RestResource(urlMapping='/CustomRestApi/*')
global class CustomRestApi {

    @HttpPost
    global static string sayHello(String name) {
        return 'Hello '+ name;
    }
}
```

You can call the custom Apex REST methods by using the following code:
```C#
Coroutine<String> routine = this.StartCoroutine<String>(
    sfdcClient.runApex("POST", "CustomRestApi", "{\"name\": \"world\"}", "")
);
yield return routine.coroutine;
string result = routine.getValue(); // Hello world
```

## Dependencies

**JSONObject**

This project relies on JSONObject 1.4, a simple C# JSON parser.<br/>
Copyright (C) 2012 Boomlagoon Ltd.

**Coroutine extension**

As a convenience, this project includes a utility class that provide Coroutines with supports for return values and exceptions.
It is important to always call the `getValue()` method even if there is no expected result as this triggers exceptions.

```C#
Coroutine<bool> routine = this.StartCoroutine<bool>(
  sfClient.login(SFDC_USERNAME, SFDC_PASSWORD)
);
yield return routine.coroutine;
try {
  bool isUserLoggedIn = routine.getValue();
}
catch (Exception e) {
  // Handle exception
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
- updated project to use UnityWebRequest
- reduced complexity of CRUD operations on salesforce records (see SalesforceRecord class)