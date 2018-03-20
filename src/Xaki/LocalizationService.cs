﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xaki.LanguageResolvers;

namespace Xaki
{
    public class LocalizationService : ILocalizationService
    {
        public const string FallbackLanguageCode = "en";

        public IEnumerable<string> LanguageCodes { get; set; } = new[] { FallbackLanguageCode };
        public ILanguageResolver LanguageResolver { get; set; } = new StaticLanguageResolver(FallbackLanguageCode);

        /// <summary>
        /// Serializes a localized content <see cref="IDictionary{TKey,TValue}"/> to JSON.
        /// </summary>
        public string Serialize(IDictionary<string, string> content)
        {
            var item = new JObject();

            foreach (var languageCode in LanguageCodes)
            {
                if (content.TryGetValue(languageCode, out var value))
                {
                    item[languageCode.ToLowerInvariant()] = value;
                }
            }

            return item.ToString(Formatting.None);
        }

        /// <summary>
        /// Deserializes JSON localized content to <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        public IDictionary<string, string> Deserialize(string json)
        {
            var item = JObject.Parse(json);

            return LanguageCodes
                .Where(i => item[i] != null)
                .ToDictionary(i => i, i => (string)item[i]);
        }

        /// <summary>
        /// Deserializes a serialized collection of <see cref="IDictionary{TKey,TValue}"/> items, returns a new collection with default languageCode if JSON reader exception occurs.
        /// </summary>
        /// <param name="json">JSON serialized localized content.</param>
        /// <param name="localizedContent">A <see cref="IDictionary{TKey,TValue}"/> of localized content.</param>
        public bool TryDeserialize(string json, out IDictionary<string, string> localizedContent)
        {
            try
            {
                localizedContent = Deserialize(json);

                return true;
            }
            catch (Exception ex) when (ex is JsonReaderException || ex is JsonSerializationException)
            {
                localizedContent = new Dictionary<string, string>
                {
                    { LanguageCodes.First(), "" }
                };

                return false;
            }
        }

        /// <summary>
        /// Localizes all properties on an <see cref="ILocalizable"/> item with the language code provided by <see cref="ILanguageResolver"/>.
        /// </summary>
        public T Localize<T>(T item) where T : class, ILocalizable
        {
            var languageCode = LanguageResolver.GetLanguageCode();

            return Localize(item, languageCode);
        }

        /// <summary>
        /// Localizes all properties on an <see cref="ILocalizable"/> item with the specified language code.
        /// </summary>
        public T Localize<T>(T item, string languageCode) where T : class, ILocalizable
        {
            if (item == null)
            {
                return null;
            }

            if (!LanguageCodes.Contains(languageCode))
            {
                languageCode = LanguageCodes.First();
            }

            LocalizeProperties(item, languageCode);

            return item;
        }

        /// <summary>
        /// Localizes all properties on each <see cref="ILocalizable"/> item in a collection with the language code provided by <see cref="ILanguageResolver"/>.
        /// </summary>
        public IEnumerable<T> Localize<T>(IEnumerable<T> items) where T : class, ILocalizable
        {
            var languageCode = LanguageResolver.GetLanguageCode();

            return items.Select(item => Localize(item, languageCode));
        }

        /// <summary>
        /// Localizes all properties on each <see cref="ILocalizable"/> item in a collection with the specified language code.
        /// </summary>
        public IEnumerable<T> Localize<T>(IEnumerable<T> items, string languageCode) where T : class, ILocalizable
        {
            return items.Select(item => Localize(item, languageCode));
        }

        private void LocalizeProperties<T>(T item, string languageCode) where T : class, ILocalizable
        {
            var properties = item
                .GetType()
                .GetTypeInfo()
                .DeclaredProperties
                .Where(i => i.IsDefined(typeof(LocalizedAttribute)));

            foreach (var propertyInfo in properties)
            {
                var propertyValue = propertyInfo.GetValue(item)?.ToString();
                if (string.IsNullOrEmpty(propertyValue))
                {
                    continue;
                }

                if (!TryDeserialize(propertyValue, out var localizedContents))
                {
                    continue;
                }

                var contentForLanguage = GetContentForLanguage(localizedContents, languageCode);
                propertyInfo.SetValue(item, contentForLanguage, null);
            }
        }

        private string GetContentForLanguage(IDictionary<string, string> localizedContents, string languageCode)
        {
            if (!localizedContents.Any())
            {
                throw new ArgumentException("Cannot localize property, no localized property values exist.", nameof(localizedContents));
            }

            var localizedContent = localizedContents.SingleOrDefault(i => i.Key.Equals(languageCode, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(i.Value)).Value;

            return localizedContent ?? GetContentForFirstLanguage(localizedContents);
        }

        private string GetContentForFirstLanguage(IDictionary<string, string> localizedContents)
        {
            return localizedContents.SingleOrDefault(i => i.Key.Equals(LanguageCodes.First())).Value ??
                   localizedContents.First().Value;
        }
    }
}
