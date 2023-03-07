using System;
using System.Collections.Generic;

namespace TeramedQRTool.Helper
{
    public static class StringParameterParser
    {
        public static string Parse(string paramsStr, Func<string, string> dataFetchFun)
        {
            var dict = new Dictionary<string, string>();
            var restStr = paramsStr;

            // 取出參數
            while (restStr.Length != 0)
            {
                var keywordStart = restStr.IndexOf("{", StringComparison.Ordinal);
                if (keywordStart == -1) break;

                var keywordEnd = restStr.IndexOf("}", StringComparison.Ordinal);
                var parameter = restStr.Substring(keywordStart, keywordEnd - keywordStart + 1);

                if (dict.ContainsKey(parameter))
                {
                    restStr = restStr.Substring(keywordEnd + 1);
                    continue;
                }
                
                dict.Add(parameter, parameter.Replace("{", "").Replace("}", ""));
                restStr = restStr.Substring(keywordEnd + 1);
            }

            // 套用
            var result = paramsStr;
            foreach (var pairData in dict)
            {
                var parameter = pairData.Key;
                var value = pairData.Value;
                var str = dataFetchFun(value);
                result = result.Replace(parameter, str);
            }

            return result;
        }
    }
}