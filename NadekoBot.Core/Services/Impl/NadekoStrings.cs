using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace NadekoBot.Core.Services.Impl
{
    public class NadekoStrings : INService
    {
        public const string stringsPath = @"_strings/";

        private readonly ImmutableDictionary<string, ImmutableDictionary<string, object>> responseStrings;
        private readonly Logger _log;
        /// <summary>
        /// Used as failsafe in case response key doesn't exist in the selected or default language.
        /// </summary>
        private readonly CultureInfo _usCultureInfo = new CultureInfo("en-US");
        private readonly ILocalization _localization;
        private readonly Random _random;

        public NadekoStrings(ILocalization loc)
        {
            _random = new Random();
            _log = LogManager.GetCurrentClassLogger();
            _localization = loc;

            var sw = Stopwatch.StartNew();
            var allLangsDict = new Dictionary<string, ImmutableDictionary<string, object>>(); // lang:(name:value)
            foreach (var file in Directory.GetFiles(stringsPath))
            {
                var langDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(file));

                allLangsDict.Add(GetLocaleName(file).ToUpperInvariant(), langDict.ToImmutableDictionary());
            }

            responseStrings = allLangsDict.ToImmutableDictionary();
            sw.Stop();

            _log.Info("Loaded {0} languages in {1:F2}s",
                responseStrings.Count,
                sw.Elapsed.TotalSeconds);

            ////improper string format checks
            //var compareTo = responseStrings["en-us"]
            //    .Select(x =>
            //    {
            //        return (StringKey: x.Key, Placeholders: formatFinder.Matches(x.Value).Cast<Match>().Select(y => y.Value).ToArray());
            //    })
            //    .ToDictionary(x => x.StringKey, x => x.Placeholders);

            //var errors = responseStrings
            //    .Select(a => (a.Key, a.Value.Select(x =>
            //        {
            //            if (!compareTo.ContainsKey(x.Key))
            //                return (StringKey: x.Key, Placeholders: new HashSet<string>(), Missing: true);
            //            var hs = new HashSet<string>(compareTo[x.Key]);
            //            hs.SymmetricExceptWith(formatFinder.Matches(x.Value).Cast<Match>().Select(y => y.Value).ToArray());
            //            return (StringKey: x.Key, Placeholders: hs, Missing: false);
            //        })
            //        .Where(x => x.Placeholders.Any() || x.Missing)))
            //    .Where(x => x.Item2.Any());

            //var str = string.Join("\n", errors.Select(x => $"------{x.Item1}------\n" +
            //                            string.Join("\n", x.Item2.Select(y =>
            //                                y.StringKey + ": " + (y.Missing ? "MISSING" : string.Join(", ", y.Placeholders))))));
            //if (!string.IsNullOrWhiteSpace(str))
            //    _log.Warn($"Improperly Formatted strings:\n{str}");
        }

        private static string GetLocaleName(string fileName)
        {
            var dotIndex = fileName.IndexOf('.') + 1;
            var secondDotINdex = fileName.LastIndexOf('.');
            return fileName.Substring(dotIndex, secondDotINdex - dotIndex);
        }

        private string GetString(string text, CultureInfo cultureInfo)
        {
            if (!responseStrings.TryGetValue(cultureInfo.Name.ToUpperInvariant(), out ImmutableDictionary<string, object> strings))
                return null;

            strings.TryGetValue(text, out object val);
            if(val == null)
                return null;
            if(val is string)
                return (string)val;
            if(!(val is JArray)) {
                LogManager.GetCurrentClassLogger().Warn(text 
                                                        + " = "
                                                        + val
                                                        +" key from " 
                                                        + cultureInfo 
                                                        + " response strings has incorrect type " 
                                                        + val.GetType() 
                                                        + ". PLEASE REPORT THIS.");
                return null;
            }
            JArray arr = (JArray)val;
            if(arr.Length == 0)
                return null;
            var arrKey = _random.Next(0, arr.Length);
            object newVal = arr[arrKey];
            if(newVal == null)
                return null;
            if(!(newVal is string)) {
                LogManager.GetCurrentClassLogger().Warn(text 
                                                        + "["
                                                        + arrKey
                                                        + "] = "
                                                        + newVal
                                                        + " key from " 
                                                        + cultureInfo 
                                                        + " response strings has incorrect type " 
                                                        + val.GetType() 
                                                        + ". PLEASE REPORT THIS.");
                return null;
            }
            return (string)newVal;
        }

        public string GetText(string key, ulong? guildId, string lowerModuleTypeName, params object[] replacements) =>
            GetText(key, _localization.GetCultureInfo(guildId), lowerModuleTypeName, replacements);

        public string GetText(string key, CultureInfo cultureInfo, string lowerModuleTypeName)
        {
            var text = GetString(lowerModuleTypeName + "_" + key, cultureInfo);

            if (string.IsNullOrWhiteSpace(text))
            {
                LogManager.GetCurrentClassLogger().Warn(lowerModuleTypeName + "_" + key + " key is missing from " + cultureInfo + " response strings. PLEASE REPORT THIS.");
                text = GetString(lowerModuleTypeName + "_" + key, _usCultureInfo) ?? $"Error: dkey {lowerModuleTypeName + "_" + key} not found!";
                if (string.IsNullOrWhiteSpace(text))
                    return "I can't tell you if the command is executed, because there was an error printing out the response. Key '" +
                        lowerModuleTypeName + "_" + key + "' " + "is missing from resources. Please report this.";
            }
            return text;
        }

        public string GetText(string key, CultureInfo cultureInfo, string lowerModuleTypeName,
            params object[] replacements)
        {
            try
            {
                return string.Format(GetText(key, cultureInfo, lowerModuleTypeName), replacements);
            }
            catch (FormatException)
            {
                return "I can't tell you if the command is executed, because there was an error printing out the response. Key '" +
                       lowerModuleTypeName + "_" + key + "' " + "is not properly formatted. Please report this.";
            }
        }
    }
}
