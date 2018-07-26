using System;
using System.Collections.Generic;
using Boomlagoon.JSON;
using Salesforce;

public class Case : SalesforceRecord {

    public const string BASE_QUERY = "SELECT Id, Subject, Status FROM Case";

    public string subject { get; set; }
    public string status { get; set; }

    public Case() {}

    public Case(string id, string subject, string status) : base(id) {
        this.subject = subject;
        this.status = status;
    }

    public override string getSObjectName() {
        return "Case";
    }

    public override JSONObject toJson() {
        JSONObject record = base.toJson();
        record.Add("Subject", subject);
        record.Add("Status", status);
        return record;
    }

    public override void parseFromJson(JSONObject jsonObject) {
        base.parseFromJson(jsonObject);
        subject = jsonObject.GetString("Subject");
        status = jsonObject.GetString("Status");
    }
}
