using System.IO;
using TwelveMoons.Core.Config;
using TwelveMoons.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace TwelveMoons.EditorTools.Runtime
{
    public static class LetterSmokeTest
    {
        private const string DemoConfigDirectory = "Assets/StreamingAssets/Configs/Demo";

        [MenuItem("Twelve Moons/Tests/Run Letter Smoke Test")]
        public static void Run()
        {
            var providerRoot = Path.GetFullPath(DemoConfigDirectory);
            var csvProvider = new CsvConfigProvider(providerRoot);
            var letterTable = csvProvider.LoadTable("LetterConfig");

            if (!letterTable.TryFindById("LetterId", "letter_relief_start", out var letterRow))
            {
                throw new InvalidDataException("LetterConfig missing letter_relief_start row.");
            }

            var letter = new LetterDefinition(letterRow);
            if (letter.Title != "Relief Stores Opened" ||
                letter.SenderName != "Storehouse Clerk" ||
                string.IsNullOrEmpty(letter.BodyText))
            {
                throw new InvalidDataException("LetterConfig did not parse title, sender, or body text correctly.");
            }

            var data = new GameRuntimeData();
            data.Reset("disaster_flood_01", 18);
            data.AddLetter("letter_relief_start");
            data.AddLetter("letter_relief_prepare_end");
            data.AddLetter("letter_relief_start");

            if (data.Letters.Count != 2)
            {
                throw new InvalidDataException("Runtime letters should keep one state per received LetterId.");
            }

            var firstLetter = data.Letters[0];
            if (firstLetter.LetterId != "letter_relief_start" ||
                firstLetter.ReceivedRound != 1 ||
                firstLetter.IsRead)
            {
                throw new InvalidDataException("Runtime letter initial state is incorrect.");
            }

            firstLetter.MarkRead();
            if (!firstLetter.IsRead)
            {
                throw new InvalidDataException("Runtime letter did not mark as read.");
            }

            if (!data.RemoveLetter("letter_relief_start") ||
                data.Letters.Count != 1 ||
                data.Letters[0].LetterId != "letter_relief_prepare_end")
            {
                throw new InvalidDataException("Runtime letter did not remove the selected letter after reading.");
            }

            Debug.Log("Letter smoke test passed. LetterConfig parses title/sender/body, runtime receives multiple unique letters, ignores duplicate ids, marks selected letters as read, and removes a read letter.");
        }
    }
}
