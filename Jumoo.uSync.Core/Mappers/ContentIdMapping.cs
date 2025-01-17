﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.EntityBase;

namespace Jumoo.uSync.Core.Mappers
{
    class ContentIdMapper : IContentMapper
    {
        private string _exportRegex; 
        public ContentIdMapper(string regex)
        {
            if (!regex.IsNullOrWhiteSpace())
                _exportRegex = regex;
            else
                _exportRegex = @"\d{4,9}";
        }

        public virtual string GetExportValue(int dataTypeDefinitionId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            LogHelper.Debug<ContentIdMapper>(">> Export Value: {0}", () => value);

            Dictionary<string, string> replacements = new Dictionary<string, string>();

            foreach (Match m in Regex.Matches(value, _exportRegex))
            {
                int id;
                if (int.TryParse(m.Value, out id))
                {
                    Guid? itemGuid = GetGuidFromId(id);
                    if (itemGuid != null && !replacements.ContainsKey(m.Value))
                    {
                        replacements.Add(m.Value, itemGuid.ToString().ToLower());
                    }
                }
            }

            foreach (var pair in replacements)
            {
                value = value.Replace(pair.Key, pair.Value);
            }

            LogHelper.Debug<ContentIdMapper>("<< Export Value: {0}", () => value);
            return value;

        }

        public virtual string GetImportValue(int dataTypeDefinitionId, string content)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            string guidRegEx = @"\b[A-Fa-f0-9]{8}(?:-[A-Fa-f0-9]{4}){3}-[A-Fa-f0-9]{12}\b";

            foreach (Match m in Regex.Matches(content, guidRegEx))
            {
                var id = GetIdFromGuid(Guid.Parse(m.Value));

                if ((id != -1) && (!replacements.ContainsKey(m.Value)))
                {
                    replacements.Add(m.Value, id.ToString());
                }
            }

            foreach (KeyValuePair<string, string> pair in replacements)
            {
                content = content.Replace(pair.Key, pair.Value);
            }

            return content;
        }


        internal int GetIdFromGuid(Guid guid)
        {
            var items = ApplicationContext.Current.Services.EntityService.GetAll(UmbracoObjectTypes.Document, new [] { guid } );
            if (items != null && items.Any() && items.FirstOrDefault() != null)
                return items.FirstOrDefault().Id;

            return -1;
        }

        internal Guid? GetGuidFromId(int id)
        {
            var objectType = ApplicationContext.Current.Services.EntityService.GetObjectType(id);

            if (objectType != UmbracoObjectTypes.Unknown)
            {
                var items = ApplicationContext.Current.Services.EntityService.GetAll(objectType, id);
                if (items != null && items.Any() && items.FirstOrDefault() != null)
                    return items.FirstOrDefault().Key;
            }

            return null;
        }

    }
}
