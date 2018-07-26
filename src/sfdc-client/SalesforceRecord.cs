using System;
using System.Collections.Generic;
using Boomlagoon.JSON;

namespace Salesforce {

    public class SalesforceRecord {

        public string id { get; set; }

        protected SalesforceRecord() {}

        protected SalesforceRecord(String id) {
            this.id = id;
        }

        public virtual string getSObjectName() {
            throw new Exception("getSObjectName() shoud be overriden");
        }

        public virtual JSONObject toJson() {
            JSONObject record = new JSONObject();
            if (id != null)
                record.Add("Id", new JSONValue(id));
            return record;
        }

        public virtual void parseFromJson(JSONObject jsonObject) {
            id = jsonObject.GetString("Id");
        }

        /**
         * Loads records from a JSON array 
         **/
        public static List<T> parseFromJsonArray<T>(JSONArray array) where T : SalesforceRecord, new() {
            List<T> items = new List<T>();
            foreach (JSONValue item in array) {
                T t = new T();
                t.parseFromJson(item.Obj);
                items.Add(t);
            }
            return items;
        }
    }
}