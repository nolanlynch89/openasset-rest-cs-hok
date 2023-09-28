﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Reflection;
// serialization stuff
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OpenAsset.RestClient.Library
{
    public class RESTOptions<T> where T : Noun.Base.BaseNoun
    {
        // URL parameters
        private int _limit;
        private int _offset;
        private DateTime _ifModifiedSince;
        private List<string> _displayFields;
        private List<string> _orderBy;
        private Dictionary<string, string> _filters;

        #region Contructors
        public RESTOptions()
        {
            _limit = 0;
            _offset = 0;
            _displayFields = new List<string>();
            _orderBy = new List<string>();
            _filters = new Dictionary<string, string>();
        }
        #endregion

        #region Accessors
        public int Limit
        {
            get { return _limit; }
            set
            {
                if (value < 0)
                    _limit = 0;
                else
                    _limit = value;
            }
        }

        public int Offset
        {
            get { return _offset; }
            set
            {
                if (value < 0)
                    _offset = 0;
                else
                    _offset = value;
            }
        }

        public DateTime IfModifiedSince
        {
            get { return _ifModifiedSince; }
            set { _ifModifiedSince = value; }
        }
        #endregion

        #region OrderBy
        public void AddOrderBy(string field, bool ascending = true)
        {
            string postfix = ascending ? "Asc" : "Desc";
            field = Regex.Replace(field, "[^A-Za-z_,]", "_");
            validateParameter(field);
            //used Insert just to make it clear to add to the end of the list
            _orderBy.Insert(_orderBy.Count,field + postfix);
        }

        public bool RemoveOrderBy(string field)
        {
            field = Regex.Replace(field, "[^A-Za-z_,]", "_");
            bool result = _orderBy.Remove(field + "Asc");
            result = result || _orderBy.Remove(field + "Desc");
            return result;
        }

        public List<string> GetOrderBy()
        {
            return _orderBy;
        }

        public void ClearOrderBy()
        {
            _orderBy.Clear();
        }
        #endregion

        #region DisplayFields
        public void AddDisplayField(string field)
        {
            field = Regex.Replace(field, "[^A-Za-z_,]", "_");
            validateParameter(field);
            _displayFields.Add(field);
        }

        public bool RemoveDisplayField(string field)
        {
            field = Regex.Replace(field, "[^A-Za-z_,]", "_");
            return _displayFields.Remove(field);
        }

        public List<string> GetDisplayFields()
        {
            return _displayFields;
        }

        public void ClearDisplayFields()
        {
            _displayFields.Clear();
        }
        #endregion

        #region Search Parameters
        public void SetSearchParameter(string parameter, string value)
        {
            parameter = Regex.Replace(parameter, "[^A-Za-z_,]", "_");
            _filters[parameter] = value;
        }

        public void RemoveSearchParameter(string parameter)
        {
            parameter = Regex.Replace(parameter, "[^A-Za-z_,]", "_");
            _filters.Remove(parameter);
        }

        public string GetSearchParameter(string parameter)
        {
            parameter = Regex.Replace(parameter, "[^A-Za-z_,]", "_");
            if (_filters.ContainsKey(parameter))
                return _filters[parameter];
            return "";
        }
        #endregion

        /// <summary>
        /// https://developers.openasset.com/#url-arrays-and-objects
        /// https://stackoverflow.com/questions/75667615/how-to-filter-projects-by-custom-fields-in-openasset-api-v1
        /// Example: <your_domain>.openasset.com/REST/1/Files?limit=0&offset=0&category_id=3&filterBy[keyword_id]=2134&filterBy[employee_number]=18211
        /// </summary>
        #region Filters
        public void SetFilterBy(string parameter, string value)
        {
            parameter = $"filterBy[{parameter}]";
            _filters[parameter] = value;
        }

        public void RemoveFilterBy(string parameter)
        {
            parameter = $"filterBy[{parameter}]";
            _filters.Remove(parameter);
        }

        public string GetFilterBy(string parameter)
        {
            parameter = $"filterBy[{parameter}]";
            if (_filters.ContainsKey(parameter))
                return _filters[parameter];
            return "";
        }
        #endregion

        #region Filter Validator
        private void validateParameter(string parameter)
        {
            if (!propertyExists(parameter))
            {
                throw new NounNonExistingPropertyException(Constant.EXCEPTION_PROPERTY_NOT_EXISTS + "[" + parameter + "]");
            }
        }

        // needs to be tested for reflection speed
        private bool propertyExists(string propertyName)
        {
            List<string> result = new List<string>();
            BindingFlags allFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            List<PropertyInfo> properties = typeof(T).GetProperties(allFields)
                .Where(x => Attribute.IsDefined(x, typeof(JsonPropertyAttribute)))
                .ToList();
            foreach (PropertyInfo property in properties)
            {
                string name = (Attribute.GetCustomAttribute(property, typeof(JsonPropertyAttribute)) as JsonPropertyAttribute).PropertyName;
                if (name == null)
                    name = property.Name;
                //in case of the property being a reserved word remove the "_" in front of it
                result.Add(name);
            }
            List<FieldInfo> fields = typeof(T).GetFields(allFields)
                .Where(x => Attribute.IsDefined(x, typeof(JsonPropertyAttribute)))
                .ToList();
            foreach (FieldInfo field in fields)
            {
                string name = (Attribute.GetCustomAttribute(field, typeof(JsonPropertyAttribute)) as JsonPropertyAttribute).PropertyName;
                if (name == null)
                    name = field.Name;
                //in case of the property being a reserved word remove the "_" in front of it
                result.Add(name);
            }
            return result.Contains(propertyName);
        }
        #endregion

        public string GetUrlParameters()
        {
            return GetUrlParameters(this.GetPostParameters());
        }

        public static string GetUrlParameters(Dictionary<string, object> parameters)
        {
            List<string> paramList = new List<string>();
            foreach (KeyValuePair<string, object> kvp in parameters)
            {
                paramList.Add(kvp.Key + "=" + HttpUtility.UrlEncode(kvp.Value.ToString()));
            }

            return String.Join("&", paramList.ToArray());
        }

        public Dictionary<string, object> GetPostParameters()
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters["limit"] =  Limit;
            parameters["offset"] = Offset;
            if (_displayFields.Count > 0)
                parameters["displayFields"] = String.Join(",", _displayFields.ToArray());
            if (_orderBy.Count > 0)
                parameters["orderBy"] = String.Join(",", _orderBy.ToArray());
            foreach (KeyValuePair<string, string> kvp in _filters)
            {
                parameters[kvp.Key] = kvp.Value;
            }

            return parameters;
        }

        public void SetLastModified(Connection conn)
        {
            this.SetLastModified(conn.LastResponseHeaders);
        }

        public void SetLastModified(Connection.ResponseHeaders headers)
        {
            if (headers.LastModified != DateTime.MinValue)
                this.IfModifiedSince = headers.LastModified;
            else
                this.IfModifiedSince = headers.Date;
        }
       
        public void SetLastModified<T>(List<T> nouns)
        {
            this.IfModifiedSince = DateTime.MinValue;
            foreach (T noun in nouns)
            {
                Noun.Base.IUpdatedNoun updatedNoun = noun as Noun.Base.IUpdatedNoun;
                if (updatedNoun == null)
                    continue;
                if (updatedNoun.Updated > this.IfModifiedSince)
                    this.IfModifiedSince = updatedNoun.Updated;
            }
        }
    }
}
