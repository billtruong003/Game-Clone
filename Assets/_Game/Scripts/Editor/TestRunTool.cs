using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace CasualGame.EditorTools
{
    /// <summary>Runs the EditMode suite and writes a plain-text report to Temp/test-results.txt (for automation).</summary>
    public static class TestRunTool
    {
        public const string ReportPath = "Temp/test-results.txt";

        [MenuItem("Tools/Casual Game/Run EditMode Tests")]
        public static void RunEditMode()
        {
            File.Delete(ReportPath);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Writer());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "CasualGame.Tests.EditMode" } }));
        }

        private class Writer : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor r)
            {
                if (!r.HasChildren) File.AppendAllText(ReportPath, $"{r.Test.Name}: {r.TestStatus} {r.Duration:0.0}s {r.Message}\n");
            }

            public void RunFinished(ITestResultAdaptor r) =>
                File.AppendAllText(ReportPath, $"DONE passed={r.PassCount} failed={r.FailCount}\n");
        }
    }
}
