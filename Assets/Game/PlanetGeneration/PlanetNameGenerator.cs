using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public static class DictionaryExtension
{
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
        TKey key, Func<TValue> valueCreator)
    {
        TValue value;
        if (!dictionary.TryGetValue(key, out value))
        {
            value = valueCreator();
            dictionary.Add(key, value);
        }
        return value;
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
        TKey key) where TValue : new()
    {
        return dictionary.GetOrAdd(key, () => new TValue());
    }
}

public class PlanetNameGenerator
{
    private static bool Initialized = false;

    private static TextAsset MarkovDB;

    private static Dictionary<char, Dictionary<char, int>> MarkovChain = new Dictionary<char, Dictionary<char, int>>();

    private static void Init()
    {
        Initialized = true;

        MarkovDB = Resources.Load<TextAsset>("planet-names");

        char PrevChar = '!';
        for(int i = 0; i < MarkovDB.text.Length; i++)
        {
            char ch = MarkovDB.text[i];
            
            int Idx = MarkovChain.GetOrAdd(PrevChar).GetOrAdd(ch);
            MarkovChain[PrevChar][ch] = Idx + 1;

            PrevChar = ch;
        }
    }

    public static string GenerateName(Random rng, int MinLen)
    {
        if (!Initialized)
            Init();
            
        var UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        char CurChar = UpperChars[rng.Next(0, UpperChars.Length)];
        
        while(!MarkovChain.ContainsKey(CurChar))
        {
            CurChar = UpperChars[rng.Next(0, UpperChars.Length)];
        }

        string CurStr = "";
        
        for (int i = 0; i < 10; i++)
        {
            if (CurChar == '\n')
                break;

            CurStr += CurChar;
            var NextTable = MarkovChain[CurChar];
            int TableSize = 0;
            foreach (var TableElem in NextTable)
            {
                TableSize += TableElem.Value;
            }

            var Idx = rng.Next(TableSize);
            
            int TableIndexer = 0;

            int NumRetries = 0;
            
            for (int j = 0; j < NextTable.Count; j++)
            {
                NumRetries++;
                var TableElem = NextTable.ElementAt(j);
                
                TableIndexer += TableElem.Value;
                if (TableIndexer > Idx)
                {
                    CurChar = TableElem.Key;
                    if (CurChar == '\n' && CurStr.Length < MinLen && NumRetries < 4096)
                    {
                        Idx = rng.Next(TableSize);
                        j = 0;
                        TableIndexer = 0;
                        continue;
                    }
                    break;
                }
            }
        }
        
        return CurStr;
    }
}
