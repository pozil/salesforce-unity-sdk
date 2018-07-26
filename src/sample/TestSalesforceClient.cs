using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Salesforce;
using Boomlagoon.JSON;

[RequireComponent(typeof(SalesforceClient))]
public class TestSalesforceClient : MonoBehaviour {

    public string salesforceUsername = "admin@sdg17.org";
    public string salesforcePassword = "5lUT!qQ$*XZ2";

    IEnumerator Start() {
        // Get Salesforce client component
        SalesforceClient sfdcClient = GetComponent<SalesforceClient>();

        // Init client & log in
        Coroutine<bool> loginRoutine = this.StartCoroutine<bool>(
            sfdcClient.login(salesforceUsername, salesforcePassword)
        );
        yield return loginRoutine.coroutine;
        try {
            loginRoutine.getValue();
            Debug.Log("Salesforce login successful.");
        }
        catch (SalesforceConfigurationException e) {
            Debug.Log("Salesforce login failed due to invalid auth configuration");
            throw e;
        }
        catch (SalesforceAuthenticationException e) {
            Debug.Log("Salesforce login failed due to invalid credentials");
            throw e;
        }
        catch (SalesforceApiException e) {
            Debug.Log("Salesforce login failed");
            throw e;
        }

        // Get some cases
        string query = Case.BASE_QUERY + " ORDER BY Subject LIMIT 5";
        Coroutine<List<Case>> getCasesRoutine = this.StartCoroutine<List<Case>>(
            sfdcClient.query<Case>(query)
        );
        yield return getCasesRoutine.coroutine;
        List<Case> cases = getCasesRoutine.getValue();
        Debug.Log("Retrieved " + cases.Count + " cases");

        // Create sample case
        Case caseRecord = new Case(null, "Test case", "New");
        Coroutine<Case> insertCaseRoutine = this.StartCoroutine<Case>(
            sfdcClient.insert(caseRecord)
        );
        yield return insertCaseRoutine.coroutine;
        insertCaseRoutine.getValue();
        Debug.Log("Case created");

        // Update sample case
        caseRecord.subject = "Updated test case";
        caseRecord.status = "Closed";
        Coroutine<string> updateCaseRoutine = this.StartCoroutine<string>(
            sfdcClient.update(caseRecord)
        );
        yield return updateCaseRoutine.coroutine;
        updateCaseRoutine.getValue();
        Debug.Log("Case updated");

        // Delete sample case
        Coroutine<string> deleteCaseRoutine = this.StartCoroutine<string>(
            sfdcClient.delete(caseRecord)
        );
        yield return deleteCaseRoutine.coroutine;
        deleteCaseRoutine.getValue();
        Debug.Log("Case deleted");
    }
}
